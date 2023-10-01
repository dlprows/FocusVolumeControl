﻿using System;
using System.Collections.Generic;
using System.Linq;
using BarRaider.SdTools;
using System.Drawing;
using System.Runtime.InteropServices;

namespace FocusVolumeControl.AudioSessions;

public sealed class ActiveAudioSessionWrapper : IAudioSession
{
	public string DisplayName { get; set; }
	public string ExecutablePath { get; set; }
	public string IconPath { get; set; }
	private List<IAudioSessionControl2> Sessions { get; } = new List<IAudioSessionControl2>();
	private IEnumerable<ISimpleAudioVolume> Volume => Sessions.Cast<ISimpleAudioVolume>();

	string _icon;

	public string GetIcon()
	{
		if (string.IsNullOrEmpty(_icon))
		{
			try
			{
				if(!string.IsNullOrEmpty(IconPath))
				{
					var tmp = (Bitmap)Bitmap.FromFile(IconPath);
					tmp.MakeTransparent();
					_icon = Tools.ImageToBase64(tmp, true);
				}
				else
				{
					var tmp = Icon.ExtractAssociatedIcon(ExecutablePath);
					_icon = Tools.ImageToBase64(tmp.ToBitmap(), true);
				}
			}
			catch
			{
				_icon = "Images/encoderIcon";
			}
		}
		return _icon;
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
