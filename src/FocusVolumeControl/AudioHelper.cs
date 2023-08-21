using CoreAudio;
using FocusVolumeControl.AudioSessions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FocusVolumeControl;

public class AudioHelper
{
	IAudioSession _current;
	List<Process> _currentProcesses;

	public IAudioSession FindSession(List<Process> processes)
	{
		var deviceEnumerator = new MMDeviceEnumerator(Guid.NewGuid());

		using var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
		using var manager = device.AudioSessionManager2;

		var sessions = manager.Sessions;

		var matchingSession = new ActiveAudioSessionWrapper();

		foreach (var session in sessions)
		{
			var audioProcess = Process.GetProcessById((int)session.ProcessID);

			if (processes.Any(x => x.Id == session.ProcessID || x.ProcessName == audioProcess?.ProcessName))
			{
				try
				{
					var displayName = audioProcess.MainModule.FileVersionInfo.FileDescription;
					if (string.IsNullOrEmpty(displayName))
					{
						displayName = audioProcess.ProcessName;
					}
					matchingSession.DisplayName = displayName;
				}
				catch
				{
					matchingSession.DisplayName ??= audioProcess.ProcessName;
				}

				matchingSession.ExecutablePath ??= audioProcess.MainModule.FileName;

				//some apps like discord have multiple volume processes.
				matchingSession.AddVolume(session.SimpleAudioVolume);
			}
		}
		return matchingSession.Any() ? matchingSession : null;
	}

	static object _lock = new object();

	public IAudioSession GetActiveSession(FallbackBehavior fallbackBehavior)
	{
		lock (_lock)
		{
			var processes = GetPossibleProcesses();

			if (_currentProcesses == null || !_currentProcesses.SequenceEqual(processes))
			{
				_current = FindSession(processes);
			}

			if(_current == null)
			{
				if(fallbackBehavior == FallbackBehavior.SystemSounds)
				{
					_current = GetSystemSounds();
				}
				else if(fallbackBehavior == FallbackBehavior.SystemVolume)
				{
					_current = GetSystemVolume();
				}
			}

			_currentProcesses = processes;
			return _current;
		}
	}

	/// <summary>
	/// Get the list of processes that might be currently selected
	/// This includes getting the child window's processes
	///
	/// This helps to find the audo process for windows store apps whose process is "ApplicationFrameHost.exe"
	///
	/// The list may optionally include a parent process, because that helps thing steam to be more reliable because the steamwebhelper (ui) is a child of steam.exe
	///
	/// According to deej, getting the ForegroundWindow and enumerating steam windows should work, but it doesn't seem to work for me without including the parent process
	/// https://github.com/omriharel/deej/blob/master/pkg/deej/util/util_windows.go#L22
	/// 
	/// but the parent process is sometimes useless (explorer, svchost, etc) so i filter some of them out because i felt like it when i wrote the code
	///
	/// I also experimented with grabbing the parent process and enumerating through the windows to see if that would help, but any time the parent process was an unexpected process (explorer) it could blow up. so i decided not to bother for now
	/// </summary>
	/// <returns></returns>
	public List<Process> GetPossibleProcesses()
	{
		var handle = Native.GetForegroundWindow();

		if (handle == IntPtr.Zero)
		{
			return null;
		}

		var ids = Native.GetProcessesOfChildWindows(handle);

		Native.GetWindowThreadProcessId(handle, out var pid);
		ids.Insert(0, pid);

		var processes = ids.Distinct()
						   .Select(x => Process.GetProcessById(x))
						   .ToList();

		try
		{
			var blah = ParentProcessUtilities.GetParentProcess(pid);
			if (blah != null && blah.ProcessName != "explorer" && blah.ProcessName != "svchost")
			{
				processes.Add(blah);
			}
		}
		catch
		{
		}

		return processes;

	}

	public void ResetAll()
	{
		try
		{
			var deviceEnumerator = new MMDeviceEnumerator(Guid.NewGuid());

			using var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
			using var manager = device.AudioSessionManager2;

			foreach (var session in manager.Sessions)
			{
				session.SimpleAudioVolume.MasterVolume = 1;
				session.SimpleAudioVolume.Mute = false;
			}
		}
		catch { }
	}

	public IAudioSession GetSystemSounds()
	{
		var deviceEnumerator = new MMDeviceEnumerator(Guid.NewGuid());

		using var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
		using var manager = device.AudioSessionManager2;

		var sessions = manager.Sessions;

		foreach (var session in sessions)
		{
			if (session.IsSystemSoundsSession)
			{
				return new SystemSoundsAudioSession(session.SimpleAudioVolume);
			}
		}

		return null;
	}
	public IAudioSession GetSystemVolume()
	{
		var deviceEnumerator = new MMDeviceEnumerator(Guid.NewGuid());

		using var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
		return new SystemVolumeAudioSession(device.AudioEndpointVolume);
	}

}
