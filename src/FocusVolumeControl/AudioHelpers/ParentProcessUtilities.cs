using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FocusVolumeControl.AudioHelpers;

/// <summary>
/// A utility class to determine a process parent.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ParentProcessUtilities
{
	// These members must match PROCESS_BASIC_INFORMATION
	internal IntPtr Reserved1;
	internal IntPtr PebBaseAddress;
	internal IntPtr Reserved2_0;
	internal IntPtr Reserved2_1;
	internal IntPtr UniqueProcessId;
	internal IntPtr InheritedFromUniqueProcessId;

	
	/// <summary>
	/// Gets the parent process of specified process.
	/// </summary>
	/// <param name="id">The process id.</param>
	/// <returns>An instance of the Process class.</returns>
	public static Process GetParentProcess(int id)
	{
		var process = Process.GetProcessById(id);
		return GetParentProcess(process);
	}

	/// <summary>
	/// Gets the parent process of a specified process.
	/// </summary>
	/// <param name="handle">The process handle.</param>
	/// <returns>An instance of the Process class.</returns>
	public static Process GetParentProcess(Process process)
	{
		var data = new ParentProcessUtilities();
		int status = Native.NtQueryInformationProcess(process.Handle, 0, ref data, Marshal.SizeOf(data), out var returnLength);
		if (status != 0)
		{
			return null;
		}

		try
		{
			return Process.GetProcessById(data.InheritedFromUniqueProcessId.ToInt32());
		}
		catch
		{
			return null;
		}
	}

}
