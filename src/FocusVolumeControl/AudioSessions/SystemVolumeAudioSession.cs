using System;
using System.Runtime.InteropServices;

namespace FocusVolumeControl.AudioSessions;

internal sealed class SystemVolumeAudioSession : IAudioSession
{
	public SystemVolumeAudioSession(IAudioEndpointVolume volumeControl)
	{
		_volumeControl = volumeControl;
	}

	IAudioEndpointVolume _volumeControl;

	public string DisplayName => "System Volume";
	public string GetIcon() => "Images/encoderIcon";

	public void ToggleMute()
	{
		_volumeControl.SetMute(!IsMuted(), Guid.Empty);
	}

	public bool IsMuted()
	{
		_volumeControl.GetMute(out var mute);
		return mute;
	}

	public void IncrementVolumeLevel(int step, int ticks)
	{
		_volumeControl.GetMasterVolumeLevelScalar(out var level);
		level = VolumeHelpers.GetAdjustedVolume(level, step, ticks);
		_volumeControl.SetMasterVolumeLevelScalar(level, Guid.Empty);
	}

	public int GetVolumeLevel()
	{
		_volumeControl.GetMasterVolumeLevelScalar(out var level);
		return VolumeHelpers.GetVolumePercentage(level);
	}
}
