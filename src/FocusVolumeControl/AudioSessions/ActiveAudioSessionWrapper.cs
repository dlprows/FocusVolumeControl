using CoreAudio;
using System;
using System.Collections.Generic;
using System.Linq;
using BarRaider.SdTools;
using System.Drawing;

namespace FocusVolumeControl.AudioSessions;

public class ActiveAudioSessionWrapper : IAudioSession
{
	public string DisplayName { get; set; }
	public string ExecutablePath { get; set; }
	private List<SimpleAudioVolume> Volume { get; } = new List<SimpleAudioVolume>();

	string _icon;

	public string GetIcon()
	{
		if (string.IsNullOrEmpty(_icon))
		{
			try
			{
				var tmp = Icon.ExtractAssociatedIcon(ExecutablePath);
				_icon = Tools.ImageToBase64(tmp.ToBitmap(), true);
			}
			catch
			{
				_icon = "Image/encoderIcon";
			}
		}
		return _icon;
	}

	public bool Any()
	{
		return Volume.Any();
	}
	public int Count => Volume.Count;

	public void AddVolume(SimpleAudioVolume volume)
	{
		Volume.Add(volume);
	}

	public void ToggleMute()
	{
		//when all volumes are muted, Volume.All will return true
		//so we swap from muted to false (opposite of Volume.All)

		//when any volumes are unmuted, Volume.All will return false
		//so we set muted to true (opposite of Volume.All)

		var muted = Volume.All(x => x.Mute);

		Volume.ForEach(x => x.Mute = !muted);
	}

	public bool IsMuted()
	{
		return Volume.All(x => x.Mute);
	}

	public void IncrementVolumeLevel(int step, int ticks)
	{
		//if you have more than one volume. they will all get set based on the first volume control
		var level = Volume.FirstOrDefault()?.MasterVolume ?? 0;

		level += (0.01f * step) * ticks;
		level = Math.Max(level, 0);
		level = Math.Min(level, 1);

		Volume.ForEach(x => x.MasterVolume = level);
	}

	public int GetVolumeLevel()
	{
		var level = Volume.FirstOrDefault()?.MasterVolume ?? 0;
		return (int)(level * 100);
	}

}
