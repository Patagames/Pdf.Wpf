using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

namespace Patagames.Pdf.Net.Controls.Wpf
{
	internal class DispatcherISyncInvoke : ISynchronizeInvoke
	{
		#region Internal IAsync Class
		private class DispatcherOperationAsync : IAsyncResult, IDisposable
		{
			private readonly DispatcherOperation _dop;
			private ManualResetEvent _handle = new ManualResetEvent(false);

			#region Implementation of IAsyncResult

			public DispatcherOperationAsync(DispatcherOperation dispatcherOperation)
			{
				_dop = dispatcherOperation;
				_dop.Aborted += DopAborted;
				_dop.Completed += DopCompleted;
			}
			public object Result
			{
				get
				{
					if (!IsCompleted)
						throw new InvalidAsynchronousStateException("Not Completed");
					return _dop.Result;
				}
			}
			void DopCompleted(object sender, EventArgs e)
			{
				_handle.Set();
			}

			void DopAborted(object sender, EventArgs e)
			{
				_handle.Set();
			}
			public bool IsCompleted
			{
				get { return _dop.Status == DispatcherOperationStatus.Completed; }
			}

			public WaitHandle AsyncWaitHandle
			{
				get { return _handle; }
			}

			public object AsyncState
			{
				get
				{
					//Not Implementted
					return null;
				}
			}

			public bool CompletedSynchronously
			{
				get { return false; }
			}

			#endregion

			#region Implementation of IDisposable

			public void Dispose()
			{
				if (_handle == null) return;
#if DOTNET30
#elif DOTNET35
#else
				_handle.Dispose();
#endif
				_handle = null;
			}

#endregion
		}
#endregion

		private readonly Dispatcher _dispatcher;
	
#region Implementation of ISynchronizeInvoke

		public DispatcherISyncInvoke(Dispatcher dispatcher)
		{
			_dispatcher = dispatcher;
		}
		public IAsyncResult BeginInvoke(Delegate method, object[] args)
		{
			return new DispatcherOperationAsync(_dispatcher.BeginInvoke(method, args));
		}

		public object EndInvoke(IAsyncResult result)
		{
			result.AsyncWaitHandle.WaitOne();
			if (result is DispatcherOperationAsync)
				return ((DispatcherOperationAsync)result).Result;
			return null;
		}

		public object Invoke(Delegate method, object[] args)
		{
			return InvokeRequired ? EndInvoke(BeginInvoke(method, args)) : method.DynamicInvoke(args);
		}

		public bool InvokeRequired
		{

			get { return !_dispatcher.CheckAccess(); }
		}

#endregion
	}

}
