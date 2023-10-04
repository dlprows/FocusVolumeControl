using System;
using System.Collections.Generic;
using System.Linq;
using BarRaider.SdTools;
using System.Drawing;
using System.Runtime.InteropServices;
using FocusVolumeControl.UI;
using BitFaster.Caching.Lru;

namespace FocusVolumeControl.AudioSessions;

public sealed class ActiveAudioSessionWrapper : IAudioSession
{
	static ConcurrentLru<string, string> _iconCache = new ConcurrentLru<string, string>(10);

	public string DisplayName { get; set; }
	public string ExecutablePath { get; set; }
	public string IconPath { get; set; }
	private List<IAudioSessionControl2> Sessions { get; } = new List<IAudioSessionControl2>();
	private IEnumerable<ISimpleAudioVolume> Volume => Sessions.Cast<ISimpleAudioVolume>();

	string GetIconFromIconPath()
	{
		return _iconCache.GetOrAdd(IconPath, (key) =>
		{
			var tmp = (Bitmap)Bitmap.FromFile(IconPath);
			tmp.MakeTransparent();
			return Tools.ImageToBase64(tmp, true);
		});
	}

	string GetIconFromExecutablePath()
	{
		return _iconCache.GetOrAdd(ExecutablePath, (key) =>
		{
			var tmp = IconExtraction.GetIcon(ExecutablePath);
			//var tmp = Icon.ExtractAssociatedIcon(ExecutablePath);
			return Tools.ImageToBase64(tmp, true);
		});
	}

	public string GetIcon()
	{
		try
		{
			if (!string.IsNullOrEmpty(IconPath))
			{
				return GetIconFromIconPath();
			}
			else
			{
				return GetIconFromExecutablePath();
			}
		}
		catch
		{
			return "Images/encoderIcon";
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
