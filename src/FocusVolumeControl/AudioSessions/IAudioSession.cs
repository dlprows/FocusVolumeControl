using System;

namespace FocusVolumeControl.AudioSessions;

public interface IAudioSession
{
	public string DisplayName { get; }

	public string GetIcon();

	public void ToggleMute();

	public bool IsMuted();

	public void IncrementVolumeLevel(int step, int ticks);

	public int GetVolumeLevel();
}
