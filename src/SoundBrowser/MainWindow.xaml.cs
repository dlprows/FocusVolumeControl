using FocusVolumeControl;
using FocusVolumeControl.AudioHelpers;
using FocusVolumeControl.AudioSessions;
using NHotkey;
using NHotkey.Wpf;
using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace SoundBrowser;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

	AudioHelper _audioHelper;
	Native.WinEventDelegate _delegate;
	bool _paused = false;
	bool _doOnce = false;

	public MainWindow()
	{
		InitializeComponent();
		_audioHelper = new AudioHelper();

		//normally you can just pass a lambda, but for some reason, that seems to get garbage collected
		_delegate = new Native.WinEventDelegate(WinEventProc);
		Native.RegisterForForegroundWindowChangedEvent(_delegate);

		HotkeyManager.Current.AddOrReplace("Pause", Key.P, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, OnPauseShortcut);
	}

	private void OnPauseShortcut(object? sender, HotkeyEventArgs e)
	{
		if( _paused )
		{
			_paused = false;
			_pauseButton.Content = "Running - click to pause on next app";
		}
		else
		{
			_paused = true;
			_doOnce = true;
			_pauseButton.Content = "Paused - click to resume";
			_handle = Native.GetForegroundWindow();
			DoThing();
		}
	}

	private void PauseClicked(object sender, RoutedEventArgs e)
	{
		_paused = !_paused;
		if(_paused)
		{
			_pauseButton.Content = "Pausing on next app";
			_doOnce = true;
		}
		else
		{
			_pauseButton.Content = "Running - click to pause on next app";
		}
	}

	public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
	{
		_handle = Native.GetForegroundWindow();
		DoThing();
	}

	private void DoThing()
	{
		if(_paused)
		{
			if(!_doOnce) 
			{
				return;
			}
			_doOnce = false;
			_pauseButton.Content = "Paused - click to resume";
		}
		else
		{
			_pauseButton.Content = "Running - click to pause on next app";
		}

		var sb = new StringBuilder();
		SetupCurrentAppFields(sb);
		sb.AppendLine("");
		sb.AppendLine("-------------------------------------------------------------------------------");
		sb.AppendLine("");
		DetermineDefaultDevice(sb);
		sb.AppendLine("");
		SetupAllSessionFields(sb);

		_tf.Text = sb.ToString();
		Trace.WriteLine(sb.ToString());
	}

	private void DetermineDefaultDevice(StringBuilder sb)
	{
		var deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

		deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia, out var device);

		var name = GetDeviceName(device);

		sb.AppendLine($"Default Audio Device: {name}");
	}

	IntPtr _handle;

	private void SetupCurrentAppFields(StringBuilder sb)
	{

		if (_handle != IntPtr.Zero)
		{
			//use this in debug to help there be less events

			/*
			Native.GetWindowThreadProcessId(handle, out var fpid);
			var fp = Process.GetProcessById(fpid);

			if(!fp.ProcessName.Contains("FSD"))
			{
				return;
			}
			*/

			var processes = _audioHelper.GetPossibleProcesses(_handle);
			var session = _audioHelper.FindSession(processes);

			sb.AppendLine("Possible Current Processes");
			foreach (var p in processes)
			{
				var displayName = (new NameAndIconHelper()).GetProcessInfo(p);

				sb.AppendLine($"\tpid: {p.Id}");
				sb.AppendLine($"\tprocessName: {p.ProcessName}");
				sb.AppendLine($"\tDisplayName: {displayName}");

			}

			sb.AppendLine();
			if (session != null)
			{
				sb.AppendLine("picked the following best match");
				sb.AppendLine($"\tpid: {string.Join(", ", session.Pids)}");
				sb.AppendLine($"\tsession: {session.DisplayName}");
				sb.AppendLine($"\tvolume: {session.GetVolumeLevel()}");
			}
			else
			{
				sb.AppendLine("No Match");
			}
		}

	}

	private void SetupAllSessionFields(StringBuilder sb)
	{
		var deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

		deviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active, out var deviceCollection);
		deviceCollection.GetCount(out var num);


		for (int i = 0; i < num; i++)
		{
			deviceCollection.Item(i, out var device);
			//todo: put the device name in the output
			var name = GetDeviceName(device);
			sb.AppendLine($"----{name}----");


			Guid iid = typeof(IAudioSessionManager2).GUID;
			device.Activate(ref iid, CLSCTX.ALL, IntPtr.Zero, out var m);
			var manager = (IAudioSessionManager2)m;


			manager.GetSessionEnumerator(out var sessionEnumerator);


			sessionEnumerator.GetCount(out var count);
			for (int s = 0; s < count; s++)
			{
				sessionEnumerator.GetSession(s, out var session);

				session.GetProcessId(out var processId);
				session.GetIconPath(out var path);
				var audioProcess = Process.GetProcessById(processId);
				if(audioProcess.Id == 0)
				{
					continue;
				}

				var displayName = (new NameAndIconHelper()).GetProcessInfo(audioProcess);
				sb.AppendLine($"pid: {audioProcess.Id}\t\t processName: {displayName}");
			}

			sb.Append("\n");
		}
	}

	private string GetDeviceName(IMMDevice device)
	{
		var fnkey = PKey.DeviceFriendlyName;
		device.OpenPropertyStore(EStgmAccess.STGM_READ, out var propertyStore);
		propertyStore.GetCount(out var count);
		for(int i = 0; i < count; i++)
		{
			propertyStore.GetAt(i, out var pkey);
			if(pkey.fmtId == fnkey.fmtId && pkey.PId == fnkey.PId)
			{
				propertyStore.GetValue(ref pkey, out var pvalue);

				return (string)pvalue.Value;
			}

		}
		return "";
	}

}
