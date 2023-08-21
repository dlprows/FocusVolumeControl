using CoreAudio;
using FocusVolumeControl;
using System;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace SoundBrowser;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

	AudioHelper _audioHelper;
	Native.WinEventDelegate _delegate;

	public MainWindow()
	{
		InitializeComponent();
		_audioHelper = new AudioHelper();

		//normally you can just pass a lambda, but for some reason, that seems to get garbage collected
		_delegate = new Native.WinEventDelegate(WinEventProc);
		Native.RegisterForForegroundWindowChangedEvent(_delegate);
	}

	public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
	{
		SetupCurrentAppFields();
		SetupAllSessionFields();
	}

	private void SetupCurrentAppFields()
	{
		var handle = Native.GetForegroundWindow();
		var sb = new StringBuilder();

		if (handle != IntPtr.Zero)
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

			var processes = _audioHelper.GetPossibleProcesses();
			var session = _audioHelper.FindSession(processes);

			foreach (var p in processes)
			{

				sb.AppendLine($"pid: {p.Id}");
				sb.AppendLine($"\tprocessName: {p.ProcessName}");
				try
				{
					sb.AppendLine($"\tFileDescription: {p!.MainModule!.FileVersionInfo.FileDescription}");
				}
				catch
				{
					sb.AppendLine("\tFileDescription: ##ERROR##");
				}


			}

			sb.AppendLine();
			if (session != null)
			{
				sb.AppendLine("picked the following best match");
				sb.AppendLine($"\tsession: {session.DisplayName}");
				sb.AppendLine($"\tvolume: {session.GetVolumeLevel()}");
			}
			else
			{
				sb.AppendLine("No Match");
			}
		}

		_tf.Text = sb.ToString();
	}

	private void SetupAllSessionFields()
	{
		_tf2.Text = "";
		var sb = new StringBuilder();
		sb.AppendLine("-------------------------------------------------------------------------------");

		var deviceEnumerator = new MMDeviceEnumerator(Guid.NewGuid());

		using var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
		using var manager = device.AudioSessionManager2;

		var sessions = manager!.Sessions;

		foreach (var session in sessions!)
		{
			var audioProcess = Process.GetProcessById((int)session.ProcessID);

			var displayName = audioProcess!.MainModule!.FileVersionInfo.FileDescription;

			sb.AppendLine($"pid: {audioProcess.Id}");
			sb.AppendLine($"\tprocessName: {audioProcess.ProcessName}");
			sb.AppendLine($"\tsession: {displayName}");
		}

		_tf2.Text = sb.ToString();
	}


}
