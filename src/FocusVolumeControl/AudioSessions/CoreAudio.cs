using System;
using System.Data;
using System.Runtime.InteropServices;

namespace FocusVolumeControl.AudioSessions;


[ComImport]
[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
public class MMDeviceEnumerator
{
}

[Flags]
public enum CLSCTX : uint
{
	INPROC_SERVER = 0x1,
	INPROC_HANDLER = 0x2,
	LOCAL_SERVER = 0x4,
	INPROC_SERVER16 = 0x8,
	REMOTE_SERVER = 0x10,
	INPROC_HANDLER16 = 0x20,
	RESERVED1 = 0x40,
	RESERVED2 = 0x80,
	RESERVED3 = 0x100,
	RESERVED4 = 0x200,
	NO_CODE_DOWNLOAD = 0x400,
	RESERVED5 = 0x800,
	NO_CUSTOM_MARSHAL = 0x1000,
	ENABLE_CODE_DOWNLOAD = 0x2000,
	NO_FAILURE_LOG = 0x4000,
	DISABLE_AAA = 0x8000,
	ENABLE_AAA = 0x10000,
	FROM_DEFAULT_CONTEXT = 0x20000,
	INPROC = INPROC_SERVER | INPROC_HANDLER,
	SERVER = INPROC_SERVER | LOCAL_SERVER | REMOTE_SERVER,
	ALL = SERVER | INPROC_HANDLER
}

public enum DataFlow
{
	Render,
	Capture,
	All,
}

public enum Role
{
	Console,
	Multimedia,
	Communications,
}

[Flags]
public enum DeviceState : uint
{
	Active = 1 << 0,
	Disabled = 1 << 1,
	NotPresent = 1 << 2,
	Unplugged = 1 << 3,
	MaskAll = 0xFu
}

public enum AudioSessionState
{
	AudioSessionStateInactive = 0,
	AudioSessionStateActive = 1,
	AudioSessionStateExpired = 2
}


[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDeviceCollection
{
	[PreserveSig]
	int GetCount(out int nDevices);

	[PreserveSig]
	int Item(int nDevice, out IMMDevice Device);
}

[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDeviceEnumerator
{

	[PreserveSig]
	int EnumAudioEndpoints(DataFlow dataFlow, DeviceState StateMask, out IMMDeviceCollection deviceCollection);

	[PreserveSig]
	int GetDefaultAudioEndpoint(DataFlow dataFlow, Role role, out IMMDevice device);
}

[Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDevice
{
	[PreserveSig]
	int Activate(ref Guid iid, CLSCTX dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

	[PreserveSig]
	int NotImpl1();
	[PreserveSig]
	int GetId([Out, MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

}

[Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioSessionManager2
{
	int NotImpl1();
	int NotImpl2();

	[PreserveSig]
	int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);
}

[Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioSessionEnumerator
{
	[PreserveSig]
	int GetCount(out int SessionCount);

	[PreserveSig]
	int GetSession(int SessionCount, out IAudioSessionControl2 Session);
}

[Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ISimpleAudioVolume
{
	[PreserveSig]
	int SetMasterVolume(float fLevel, ref Guid EventContext);

	[PreserveSig]
	int GetMasterVolume(out float pfLevel);

	[PreserveSig]
	int SetMute(bool bMute, ref Guid EventContext);

	[PreserveSig]
	int GetMute(out bool pbMute);
}

[Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioSessionControl2
{
	//elgato seems to use this to determine whether the icon should be black and white
	[PreserveSig]
	int GetState(out AudioSessionState audioSessionState);

	[PreserveSig]
	int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

	[PreserveSig]
	int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

	[PreserveSig]
	int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

	[PreserveSig]
	int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

	[PreserveSig]
	int GetGroupingParam(out Guid pRetVal);

	[PreserveSig]
	int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

	[PreserveSig]
	int NotImpl1();

	[PreserveSig]
	int NotImpl2();

	// IAudioSessionControl2
	[PreserveSig]
	uint GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

	[PreserveSig]
	int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

	[PreserveSig]
	int GetProcessId(out int pRetVal);

	[PreserveSig]
	int IsSystemSoundsSession();

	[PreserveSig]
	int SetDuckingPreference(bool optOut);
}

// http://netcoreaudio.codeplex.com/SourceControl/latest#trunk/Code/CoreAudio/Interfaces/IAudioEndpointVolume.cs
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioEndpointVolume
{
	[PreserveSig]
	int NotImpl1();

	[PreserveSig]
	int NotImpl2();

	/// <summary>
	/// Gets a count of the channels in the audio stream.
	/// </summary>
	/// <param name="channelCount">The number of channels.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int GetChannelCount(
		[Out][MarshalAs(UnmanagedType.U4)] out UInt32 channelCount);

	/// <summary>
	/// Sets the master volume level of the audio stream, in decibels.
	/// </summary>
	/// <param name="level">The new master volume level in decibels.</param>
	/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int SetMasterVolumeLevel(
		[In][MarshalAs(UnmanagedType.R4)] float level,
		[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

	/// <summary>
	/// Sets the master volume level, expressed as a normalized, audio-tapered value.
	/// </summary>
	/// <param name="level">The new master volume level expressed as a normalized value between 0.0 and 1.0.</param>
	/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int SetMasterVolumeLevelScalar(
		[In][MarshalAs(UnmanagedType.R4)] float level,
		[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

	/// <summary>
	/// Gets the master volume level of the audio stream, in decibels.
	/// </summary>
	/// <param name="level">The volume level in decibels.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int GetMasterVolumeLevel(
		[Out][MarshalAs(UnmanagedType.R4)] out float level);

	/// <summary>
	/// Gets the master volume level, expressed as a normalized, audio-tapered value.
	/// </summary>
	/// <param name="level">The volume level expressed as a normalized value between 0.0 and 1.0.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int GetMasterVolumeLevelScalar(
		[Out][MarshalAs(UnmanagedType.R4)] out float level);

	/// <summary>
	/// Sets the volume level, in decibels, of the specified channel of the audio stream.
	/// </summary>
	/// <param name="channelNumber">The channel number.</param>
	/// <param name="level">The new volume level in decibels.</param>
	/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int SetChannelVolumeLevel(
		[In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
		[In][MarshalAs(UnmanagedType.R4)] float level,
		[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

	/// <summary>
	/// Sets the normalized, audio-tapered volume level of the specified channel in the audio stream.
	/// </summary>
	/// <param name="channelNumber">The channel number.</param>
	/// <param name="level">The new master volume level expressed as a normalized value between 0.0 and 1.0.</param>
	/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int SetChannelVolumeLevelScalar(
		[In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
		[In][MarshalAs(UnmanagedType.R4)] float level,
		[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

	/// <summary>
	/// Gets the volume level, in decibels, of the specified channel in the audio stream.
	/// </summary>
	/// <param name="channelNumber">The zero-based channel number.</param>
	/// <param name="level">The volume level in decibels.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int GetChannelVolumeLevel(
		[In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
		[Out][MarshalAs(UnmanagedType.R4)] out float level);

	/// <summary>
	/// Gets the normalized, audio-tapered volume level of the specified channel of the audio stream.
	/// </summary>
	/// <param name="channelNumber">The zero-based channel number.</param>
	/// <param name="level">The volume level expressed as a normalized value between 0.0 and 1.0.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int GetChannelVolumeLevelScalar(
		[In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
		[Out][MarshalAs(UnmanagedType.R4)] out float level);

	/// <summary>
	/// Sets the muting state of the audio stream.
	/// </summary>
	/// <param name="isMuted">True to mute the stream, or false to unmute the stream.</param>
	/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int SetMute(
		[In][MarshalAs(UnmanagedType.Bool)] Boolean isMuted,
		[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

	/// <summary>
	/// Gets the muting state of the audio stream.
	/// </summary>
	/// <param name="isMuted">The muting state. True if the stream is muted, false otherwise.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int GetMute(
		[Out][MarshalAs(UnmanagedType.Bool)] out Boolean isMuted);

	/// <summary>
	/// Gets information about the current step in the volume range.
	/// </summary>
	/// <param name="step">The current zero-based step index.</param>
	/// <param name="stepCount">The total number of steps in the volume range.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int GetVolumeStepInfo(
		[Out][MarshalAs(UnmanagedType.U4)] out UInt32 step,
		[Out][MarshalAs(UnmanagedType.U4)] out UInt32 stepCount);

	/// <summary>
	/// Increases the volume level by one step.
	/// </summary>
	/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int VolumeStepUp(
		[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

	/// <summary>
	/// Decreases the volume level by one step.
	/// </summary>
	/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int VolumeStepDown(
		[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

	/// <summary>
	/// Queries the audio endpoint device for its hardware-supported functions.
	/// </summary>
	/// <param name="hardwareSupportMask">A hardware support mask that indicates the capabilities of the endpoint.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int QueryHardwareSupport(
		[Out][MarshalAs(UnmanagedType.U4)] out UInt32 hardwareSupportMask);

	/// <summary>
	/// Gets the volume range of the audio stream, in decibels.
	/// </summary>
	/// <param name="volumeMin">The minimum volume level in decibels.</param>
	/// <param name="volumeMax">The maximum volume level in decibels.</param>
	/// <param name="volumeStep">The volume increment level in decibels.</param>
	/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
	[PreserveSig]
	int GetVolumeRange(
		[Out][MarshalAs(UnmanagedType.R4)] out float volumeMin,
		[Out][MarshalAs(UnmanagedType.R4)] out float volumeMax,
		[Out][MarshalAs(UnmanagedType.R4)] out float volumeStep);
}
