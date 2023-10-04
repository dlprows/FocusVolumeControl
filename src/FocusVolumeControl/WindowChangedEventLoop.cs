using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FocusVolumeControl
{
	internal class WindowChangedEventLoop
	{
		private static readonly Lazy<WindowChangedEventLoop> _lazy = new Lazy<WindowChangedEventLoop>(() => new WindowChangedEventLoop());
		public static WindowChangedEventLoop Instance => _lazy.Value;

		readonly Thread _thread;
		Dispatcher _dispatcher;

		IntPtr _foregroundWindowChangedEvent;
		Native.WinEventDelegate _delegate;

		private WindowChangedEventLoop()
		{
			_thread = new Thread(() =>
			{
				Logger.Instance.LogMessage(TracingLevel.DEBUG, "Starting Window Changed Event Loop");
				_delegate = new Native.WinEventDelegate(WinEventProc);
				_foregroundWindowChangedEvent = Native.RegisterForForegroundWindowChangedEvent(_delegate);

				_dispatcher = Dispatcher.CurrentDispatcher;
				Dispatcher.Run();
				Logger.Instance.LogMessage(TracingLevel.DEBUG, "Window Changed Event Loop Stopped");
			});
			_thread.SetApartmentState(ApartmentState.STA);
			_thread.Start();
		}

		public event Action WindowChanged;

		CancellationTokenSource? _cancellationTokenSource = null;

		private async void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			try
			{
				//debounce the window changed events by 100 ms because if you click mouse over an application on the start bar
				//and then click on the preview window, it will quickly go from current -> fallback -> new app
				//which can often result in it getting stuck on the fallback app
				_cancellationTokenSource?.Cancel();
				_cancellationTokenSource = new CancellationTokenSource();
				await Task.Delay(100, _cancellationTokenSource.Token);
				WindowChanged?.Invoke();
			}
			catch (TaskCanceledException)
			{
				//ignored
			}
			catch (Exception ex)
			{
				Logger.Instance.LogMessage(TracingLevel.ERROR, $"Unexpected Error in EventHandler:\n {ex}");
			}
		}
	}
}
