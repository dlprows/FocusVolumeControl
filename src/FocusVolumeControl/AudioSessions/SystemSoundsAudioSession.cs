using CoreAudio;
using System;

namespace FocusVolumeControl.AudioSessions;

internal class SystemSoundsAudioSession : IAudioSession
{
	public SystemSoundsAudioSession(SimpleAudioVolume volumeControl)
	{
		_volumeControl = volumeControl;
	}

	SimpleAudioVolume _volumeControl;

	public string DisplayName => "System sounds";
	public string GetIcon() => "Images/systemSounds";

	public void ToggleMute()
	{
		_volumeControl.Mute = !_volumeControl.Mute;
	}

	public bool IsMuted() => _volumeControl.Mute;

	public void IncrementVolumeLevel(int step, int ticks)
	{
		var level = _volumeControl.MasterVolume;

		level += (0.01f * step) * ticks;
		level = Math.Max(level, 0);
		level = Math.Min(level, 1);

		_volumeControl.MasterVolume = level;
	}

	public int GetVolumeLevel() => (int)(_volumeControl.MasterVolume * 100);

}
