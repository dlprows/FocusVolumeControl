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

		private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			try
			{
				WindowChanged?.Invoke();
			}
			catch (Exception ex)
			{
				Logger.Instance.LogMessage(TracingLevel.ERROR, $"Unexpected Error in EventHandler:\n {ex}");
			}
		}
	}
}
