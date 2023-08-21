﻿using CoreAudio;
using System;

namespace FocusVolumeControl.AudioSessions;

internal class SystemVolumeAudioSession : IAudioSession
{
	public SystemVolumeAudioSession(AudioEndpointVolume volumeControl)
	{
		_volumeControl = volumeControl;
	}

	AudioEndpointVolume _volumeControl;

	public string DisplayName => "System Volume";
	public string GetIcon() => "Images/encoderIcon";

	public void ToggleMute()
	{
		_volumeControl.Mute = !_volumeControl.Mute;
	}

	public bool IsMuted() => _volumeControl.Mute;

	public void IncrementVolumeLevel(int step, int ticks)
	{
		var level = _volumeControl.MasterVolumeLevelScalar;

		level += (0.01f * step) * ticks;
		level = Math.Max(level, 0);
		level = Math.Min(level, 1);

		_volumeControl.MasterVolumeLevelScalar = level;
	}

	public int GetVolumeLevel() => (int)(_volumeControl.MasterVolumeLevelScalar * 100);

}