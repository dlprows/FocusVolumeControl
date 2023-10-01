using FocusVolumeControl.AudioHelpers;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FocusVolumeControl;

public class Native
{
	public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

	[DllImport("user32.dll")]
	static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

	[DllImport("user32.dll")]
	public static extern bool UnhookWinEvent(IntPtr hWinEventHook);


	private const uint WINEVENT_OUTOFCONTEXT = 0;
	private const uint EVENT_SYSTEM_FOREGROUND = 3;

	public static IntPtr RegisterForForegroundWindowChangedEvent(WinEventDelegate dele) => SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);


	[DllImport("user32.dll")]
	public static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll", SetLastError = true)]
	public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

	
	private delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

	[DllImport("user32")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);


	public static List<int> GetProcessesOfChildWindows(IntPtr windowHandle)
	{
		var ids = new List<int>();

		if(windowHandle != IntPtr.Zero)
		{

		EnumChildWindows(windowHandle,
				(hWnd, lParam) => 
				{
					Native.GetWindowThreadProcessId(hWnd, out var pid);
					ids.Add(pid);

				return true;

			}, IntPtr.Zero);
		}

		return ids;
	}

	[DllImport("ntdll.dll")]
	public static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessUtilities processInformation, int processInformationLength, out int returnLength);


	[DllImport("Kernel32.dll")]
	public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint flags, StringBuilder buffer, ref uint bufferSize);

	[DllImport("kernel32.dll")]
	public static extern IntPtr OpenProcess(uint processAccess, bool inheritHandle, int processId);

	[DllImport("kernel32.dll")]
	public static extern bool CloseHandle(IntPtr hObject);


}
