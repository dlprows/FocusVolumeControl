using System;
using System.Runtime.InteropServices;

namespace FocusVolumeControl.AudioSessions;

internal sealed class SystemSoundsAudioSession : IAudioSession
{
	public SystemSoundsAudioSession(IAudioSessionControl2 sessionControl)
	{
		_sessionControl = sessionControl;
		_volumeControl = (ISimpleAudioVolume)sessionControl;
	}

	IAudioSessionControl2 _sessionControl;
	ISimpleAudioVolume _volumeControl;

	public string DisplayName => "System sounds";
	public string GetIcon() => "Images/systemSounds";

	public void ToggleMute()
	{
		var guid = Guid.Empty;
		_volumeControl.SetMute(!IsMuted(), ref guid);
	}

	public bool IsMuted()
	{
		_volumeControl.GetMute(out var mute);
		return mute;
	}

	public void IncrementVolumeLevel(int step, int ticks)
	{
		_volumeControl.GetMasterVolume(out var level);
		level = VolumeHelpers.GetAdjustedVolume(level, step, ticks);

		var guid = Guid.Empty;
		_volumeControl.SetMasterVolume(level, ref guid);
	}

	public int GetVolumeLevel()
	{
		_volumeControl.GetMasterVolume(out var level);
		return VolumeHelpers.GetVolumePercentage(level);
	}
}
