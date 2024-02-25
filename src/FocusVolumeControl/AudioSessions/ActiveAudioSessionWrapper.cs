using System;
using System.Collections.Generic;
using System.Linq;
using BarRaider.SdTools;
using System.Drawing;
using System.Runtime.InteropServices;
using FocusVolumeControl.UI;
using BitFaster.Caching.Lru;
using FocusVolumeControl.AudioSession;

namespace FocusVolumeControl.AudioSessions;

public sealed class ActiveAudioSessionWrapper : IAudioSession
{
	public string DisplayName { get; set; }
	private List<IAudioSessionControl2> Sessions { get; } = new List<IAudioSessionControl2>();
	private IEnumerable<ISimpleAudioVolume> Volume => Sessions.Cast<ISimpleAudioVolume>();

	public IconWrapper? IconWrapper { get; set; }
	public IEnumerable<int> Pids => Sessions.Select(x =>
	{
		x.GetProcessId(out var pid); return pid;
	});

	public string GetIcon()
	{
		try
		{
			return IconWrapper?.GetIconData() ?? IconWrapper.FallbackIconData;
		}
		catch
		{
			return IconWrapper.FallbackIconData;
		}
	}

	public bool Any()
	{
		return Volume.Any();
	}
	public int Count => Sessions.Count;

	public void AddSession(IAudioSessionControl2 session)
	{
		Sessions.Add(session);
	}

	public void ToggleMute()
	{
		//when all volumes are muted, Volume.All will return true
		//so we swap from muted to false (opposite of Volume.All)

		//when any volumes are unmuted, Volume.All will return false
		//so we set muted to true (opposite of Volume.All)

		var muted = IsMuted();

		foreach(var v in Volume)
		{
			var guid = Guid.Empty;
			v.SetMute(!muted, ref guid);
		}
	}

	public bool IsMuted()
	{
		return Volume.All(x =>
		{
			x.GetMute(out var mute); 
			return mute;
		});
	}

	public void IncrementVolumeLevel(int step, int ticks)
	{
		//if you have more than one volume. they will all get set based on the first volume control
		var volume = Volume.FirstOrDefault();
		var level = 0f;
		if (volume != null)
		{
			volume.GetMasterVolume(out level);
		}
	
		level = VolumeHelpers.GetAdjustedVolume(level, step, ticks);

		foreach(var v in Volume)
		{
			var guid = Guid.Empty;
			v.SetMasterVolume(level, ref guid);
		}
	}

	public int GetVolumeLevel()
	{
		var volume = Volume.FirstOrDefault();
		var level = 0f;
		if(volume != null)
		{
			volume.GetMasterVolume(out level);
		}

		return VolumeHelpers.GetVolumePercentage(level);
	}
}
