using FocusVolumeControl.AudioSessions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace FocusVolumeControl;

public class AudioHelper
{
	static object _lock = new object();
	List<Process> _currentProcesses;

	public IAudioSession Current { get; private set; }

	public void ResetCache()
	{
		lock (_lock)
		{
			Current = null;
		}
	}

	public IAudioSession FindSession(List<Process> processes)
	{
		var deviceEnumerator = (CoreAudio)new MMDeviceEnumerator();

		deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);

		Guid iid = typeof(IAudioSessionManager2).GUID;
		device.Activate(ref iid, 0, IntPtr.Zero, out var m);
		var manager = (IAudioSessionManager2)m;


		manager.GetSessionEnumerator(out var sessionEnumerator);

		var results = new ActiveAudioSessionWrapper();

		sessionEnumerator.GetCount(out var count);
		for (int i = 0; i < count; i++)
		{
			sessionEnumerator.GetSession(i, out var session);

			session.GetProcessId(out var sessionProcessId);
			var audioProcess = Process.GetProcessById(sessionProcessId);

			if (processes.Any(x => x.Id == sessionProcessId || x.ProcessName == audioProcess?.ProcessName))
			{
				try
				{
					var displayName = audioProcess.MainModule.FileVersionInfo.FileDescription;
					if (string.IsNullOrEmpty(displayName))
					{
						displayName = audioProcess.ProcessName;
					}
					results.DisplayName = displayName;
				}
				catch
				{
					results.DisplayName ??= audioProcess.ProcessName;
				}

				results.ExecutablePath ??= audioProcess.MainModule.FileName;

				//some apps like discord have multiple volume processes.
				results.AddSession(session);

			}
		}

		return results.Any() ? results : null;
	}


	public IAudioSession GetActiveSession(FallbackBehavior fallbackBehavior)
	{
		lock (_lock)
		{
			var processes = GetPossibleProcesses();

			if (_currentProcesses == null || !_currentProcesses.SequenceEqual(processes))
			{
				Current = FindSession(processes);
			}

			if (Current == null)
			{
				if (fallbackBehavior == FallbackBehavior.SystemSounds && Current is not SystemSoundsAudioSession)
				{
					Current = GetSystemSounds();
				}
				else if (fallbackBehavior == FallbackBehavior.SystemVolume && Current is not SystemVolumeAudioSession)
				{
					Current = GetSystemVolume();
				}
			}

			_currentProcesses = processes;
			return Current;
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
		var deviceEnumerator = (CoreAudio)new MMDeviceEnumerator();

		deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);

		Guid iid = typeof(IAudioSessionManager2).GUID;
		device.Activate(ref iid, 0, IntPtr.Zero, out var m);
		var manager = (IAudioSessionManager2)m;


		manager.GetSessionEnumerator(out var sessionEnumerator);

		sessionEnumerator.GetCount(out var count);
		for (int i = 0; i < count; i++)
		{
			sessionEnumerator.GetSession(i, out var session);

			var volume = (ISimpleAudioVolume)session;
			var guid = Guid.Empty;
			volume.SetMasterVolume(1, ref guid);
			volume.SetMute(false, ref guid);
		}
	}

	public IAudioSession GetSystemSounds()
	{
		var deviceEnumerator = (CoreAudio)new MMDeviceEnumerator();

		deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);

		Guid iid = typeof(IAudioSessionManager2).GUID;
		device.Activate(ref iid, 0, IntPtr.Zero, out var m);
		var manager = (IAudioSessionManager2)m;


		manager.GetSessionEnumerator(out var sessionEnumerator);

		sessionEnumerator.GetCount(out var count);
		for (int i = 0; i < count; i++)
		{
			sessionEnumerator.GetSession(i, out var session);

			if (session.IsSystemSoundsSession() == 0)
			{
				return new SystemSoundsAudioSession(session);
			}
		}
		return null;
	}
	public IAudioSession GetSystemVolume()
	{
		var deviceEnumerator = (CoreAudio)new MMDeviceEnumerator();

		deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);

		Guid iid = typeof(IAudioEndpointVolume).GUID;
		device.Activate(ref iid, 0, IntPtr.Zero, out var o);
		var endpointVolume = (IAudioEndpointVolume)o;

		return new SystemVolumeAudioSession(endpointVolume);
	}

}
