﻿using FocusVolumeControl.AudioSessions;
using FocusVolumeControl.Overrides;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace FocusVolumeControl.AudioHelpers;

public class AudioHelper
{
	NameAndIconHelper _nameAndIconHelper = new NameAndIconHelper();

	static object _lock = new object();
	int[] _currentProcesses;
	int _retryFallbackCount = 0;

	public List<Override> Overrides { get; set; }
	public List<string> Ignored { get; set; }

	public IAudioSession Current { get; private set; }

	public void ResetCache()
	{
		lock (_lock)
		{
			Current = null;
		}
	}

	private Process GetProcessById(int id)
	{
		try
		{
			return Process.GetProcessById(id);
		}
		catch
		{
			return null;
		}
	}

	public IAudioSession FindSession(List<Process> processes)
	{
		var results = new ActiveAudioSessionWrapper();
		Process bestProcessMatch = null;

		var deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

		deviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active, out var deviceCollection);
		deviceCollection.GetCount(out var numDevices);
		for (int d = 0; d < numDevices; d++)
		{
			deviceCollection.Item(d, out var device);

			if(device == null)
			{
				continue;
			}

			Guid iid = typeof(IAudioSessionManager2).GUID;
			device.Activate(ref iid, CLSCTX.ALL, IntPtr.Zero, out var m);
			var manager = (IAudioSessionManager2)m;

			device.GetId(out var currentDeviceId);

			manager.GetSessionEnumerator(out var sessionEnumerator);

			if(sessionEnumerator == null)
			{
				continue;
			}

			var currentIndex = int.MaxValue;

			sessionEnumerator.GetCount(out var count);
			for (int i = 0; i < count; i++)
			{
				sessionEnumerator.GetSession(i, out var session);

				if(session == null)
				{
					continue;
				}

				session.GetProcessId(out var sessionProcessId);
				var audioProcess = GetProcessById(sessionProcessId);

				if(audioProcess == null)
				{
					continue;
				}

				var index = processes.FindIndex(x => x.Id == sessionProcessId || x.ProcessName == audioProcess?.ProcessName);

				if (index > -1)
				{
					//processes will be ordered from best to worst (starts with the app, goes to parent)
					//so we want the display name and executable path to come from the process that is closest to the front of the list
					//but we want all matching sessions so things like discord work right
					if (index < currentIndex)
					{
						bestProcessMatch = audioProcess;
						currentIndex = index;

						if(string.IsNullOrEmpty(results.DisplayName))
						{
							session.GetDisplayName(out var displayName);
							results.DisplayName = displayName;
						}
					}


					//some apps like discord have multiple volume processes.
					//and some apps will be on multiple devices
					//so we add all sessions so we can keep them in sync
					results.AddSession(session);

				}
			}
		}

		if(bestProcessMatch != null)
		{
			_nameAndIconHelper.SetProcessInfo(bestProcessMatch, results);
		}

		return results.Any() ? results : null;
	}

	public IAudioSession GetActiveSession(FallbackBehavior fallbackBehavior)
	{
		lock (_lock)
		{
			var processes = TryGetProcessFromOverrides();

			if(processes == null)
			{
				processes = GetPossibleProcesses();
			}
			var processIds = processes?.Select(x => x.Id).ToArray();

			//_currentProcesses null - first time getting sessions
			//_currentProcesses not equal to processIds - changed the active process
			//_retryFallbackCount - some processes like chrome or minecraft will start their audio process when they first try to do some sound stuff
			if (_currentProcesses == null || !_currentProcesses.SequenceEqual(processIds) || _retryFallbackCount == 5)
			{
				_retryFallbackCount = 0;

				var newSession = FindSession(processes);

				if(newSession != null || fallbackBehavior != FallbackBehavior.PreviousApp)
				{
					Current = newSession;
				}
			}
			else if(Current is SystemSoundsAudioSession || Current is SystemVolumeAudioSession)
			{
				_retryFallbackCount++;
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

			_currentProcesses = processIds;
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
	public List<Process> GetPossibleProcesses(IntPtr? handleOverride = null)
	{
		var handle = handleOverride ?? Native.GetForegroundWindow();

		if (handle == IntPtr.Zero)
		{
			return new List<Process>();
		}

		var ids = Native.GetProcessesOfChildWindows(handle);

		Native.GetWindowThreadProcessId(handle, out var pid);

		if(pid != 0)
		{
			ids.Insert(0, pid);
		}

		if(ids.Count == 0)
		{
			return new List<Process>();
		}

		var processes = ids.Distinct()
						   .Select(Process.GetProcessById)
						   .ToList();

		if(processes.FirstOrDefault()?.ProcessName == "explorer")
		{
			return new List<Process>();
		}
		
		if(processes.Any(x => Ignored.Contains(x.ProcessName)))
		{
			return new List<Process>();
		}

		try
		{
			//note. in instances where you launch a game from steam. this ends up mapping the process to both steam and to the game. which is unfortunate
			//The problem is that if you don't use the parent processes, then the actual steam window won't get recognized. But if you do, then games will map to steam.
			//
			//Additionally, I group all audio processes that match instead of just the most specific, or the first, etc. Because Discord uses two processes, one for voice chat, and one for discord sounds.
			//
			//Steam and Discord are both very common, and end up butting heads in the algorithm.
			//I want to avoid special cases, but since steam and discord are both so common, i'm making an exception.
			var parentProcess = ParentProcessUtilities.GetParentProcess(pid);
			if (parentProcess != null 
				&& parentProcess.ProcessName != "explorer" 
				&& parentProcess.ProcessName != "svchost"
				&& (parentProcess.ProcessName == "steam" && processes.Any(x => x.ProcessName == "steamwebhelper")) //only include steam if the parent process is the steamwebhelper
				)
			{
				processes.Add(parentProcess);
			}
		}
		catch
		{
		}

		return processes;

	}

	public void ResetAll()
	{
		var deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

		deviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active, out var deviceCollection);

		deviceCollection.GetCount(out var numDevices);
		for (int d = 0; d < numDevices; d++)
		{
			deviceCollection.Item(d, out var device);

			if(device == null)
			{
				continue;
			}

			Guid iid = typeof(IAudioSessionManager2).GUID;
			device.Activate(ref iid, CLSCTX.ALL, IntPtr.Zero, out var m);
			var manager = (IAudioSessionManager2)m;


			manager.GetSessionEnumerator(out var sessionEnumerator);

			if(sessionEnumerator == null)
			{
				continue;
			}

			sessionEnumerator.GetCount(out var count);
			for (int i = 0; i < count; i++)
			{
				sessionEnumerator.GetSession(i, out var session);

				if(session == null)
				{
					continue;
				}

				var volume = (ISimpleAudioVolume)session;
				var guid = Guid.Empty;
				volume.SetMasterVolume(1, ref guid);
				volume.SetMute(false, ref guid);
			}
		}
	}

	public IAudioSession GetSystemSounds()
	{
		var deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
		deviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active, out var deviceCollection);

		deviceCollection.GetCount(out var numDevices);
		for (int d = 0; d < numDevices; d++)
		{
			deviceCollection.Item(d, out var device);

			if(device == null)
			{
				continue;
			}

			Guid iid = typeof(IAudioSessionManager2).GUID;
			device.Activate(ref iid, CLSCTX.ALL, IntPtr.Zero, out var m);
			var manager = (IAudioSessionManager2)m;


			manager.GetSessionEnumerator(out var sessionEnumerator);

			if(sessionEnumerator == null)
			{
				continue;
			}

			sessionEnumerator.GetCount(out var count);
			for (int i = 0; i < count; i++)
			{
				sessionEnumerator.GetSession(i, out var session);

				if(session == null)
				{
					continue;
				}

				if (session.IsSystemSoundsSession() == 0)
				{
					return new SystemSoundsAudioSession(session);
				}
			}
		}
		return null;
	}
	public IAudioSession GetSystemVolume()
	{
		var deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

		deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia, out var device);

		Guid iid = typeof(IAudioEndpointVolume).GUID;
		device.Activate(ref iid, CLSCTX.ALL, IntPtr.Zero, out var o);
		var endpointVolume = (IAudioEndpointVolume)o;

		return new SystemVolumeAudioSession(endpointVolume);
	}


	static Dictionary<string, List<int>> _audioSessionNameCache = new Dictionary<string, List<int>>();
	private List<int> GetFromNameCacheIfPossible(string processName)
	{
		if(_audioSessionNameCache.TryGetValue(processName, out var result))
		{
			if(result.Count!= 0)
			{
				foreach(var pid in result)
				{
					var p = GetProcessById(pid);
					if(p == null || p.ProcessName != processName)
					{
						_audioSessionNameCache.Remove(processName);
						return null;
					}
				}
				return result;
			}
		}
		return null;
	}

	private List<Process> TryGetProcessFromOverrides(IntPtr? handleOverride = null)
	{
		var handle = handleOverride ?? Native.GetForegroundWindow();

		if (Overrides?.Any() == true)
		{
			Process tmp = null;
			foreach (var p in Process.GetProcesses())
			{
				if (p.MainWindowHandle == handle)
				{
					tmp = p;
					break;
				}
			}

			if (tmp != null)
			{
				foreach (var o in Overrides)
				{
					if (
						(o.MatchType == MatchType.Equal && tmp.MainWindowTitle.Equals(o.WindowQuery, StringComparison.InvariantCultureIgnoreCase))
						|| (o.MatchType == MatchType.StartsWith && tmp.MainWindowTitle.StartsWith(o.WindowQuery, StringComparison.OrdinalIgnoreCase))
						|| (o.MatchType == MatchType.EndsWith && tmp.MainWindowTitle.EndsWith(o.WindowQuery, StringComparison.OrdinalIgnoreCase))
						|| (o.MatchType == MatchType.Regex && Regex.IsMatch(tmp.MainWindowTitle, o.WindowQuery))
						)
					{
						var ids = FindAudioSessionByProcessName(o.AudioProcessName);
						if (ids?.Count > 0)
						{
							return ids.Distinct()
								      .Select(Process.GetProcessById)
									  .ToList();

						}
					}
				}
			}
		}
		return null;
	}

	private List<int> FindAudioSessionByProcessName(string processName)
	{
		var cached = GetFromNameCacheIfPossible(processName);
		if(cached != null)
		{
			return cached;
		}

		var results = new List<int>();

		var deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

		deviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active, out var deviceCollection);
		deviceCollection.GetCount(out var numDevices);
		for (int d = 0; d < numDevices; d++)
		{
			deviceCollection.Item(d, out var device);

			if(device == null)
			{
				continue;
			}

			Guid iid = typeof(IAudioSessionManager2).GUID;
			device.Activate(ref iid, CLSCTX.ALL, IntPtr.Zero, out var m);
			var manager = (IAudioSessionManager2)m;

			device.GetId(out var currentDeviceId);

			manager.GetSessionEnumerator(out var sessionEnumerator);

			if(sessionEnumerator == null)
			{
				continue;
			}

			sessionEnumerator.GetCount(out var count);
			for (int i = 0; i < count; i++)
			{
				sessionEnumerator.GetSession(i, out var session);

				if(session == null)
				{
					continue;
				}

				session.GetDisplayName(out var displayName);
				session.GetProcessId(out var sessionProcessId);

				var audioProcess = GetProcessById(sessionProcessId);

				if(audioProcess == null)
				{
					continue;
				}

				var audioProcessName = _nameAndIconHelper.TryGetProcessNameWithoutIcon(audioProcess);
				if(audioProcess.ProcessName == processName || displayName == processName || processName == audioProcessName)
				{
					results.Add(sessionProcessId);
				}
			}
		}

		results = results.Distinct().ToList();

		_audioSessionNameCache[processName] = results;
		return results;
	}

}
