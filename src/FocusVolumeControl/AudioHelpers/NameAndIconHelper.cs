using BarRaider.SdTools;
using FocusVolumeControl.AudioSession;
using FocusVolumeControl.AudioSessions;
using FocusVolumeControl.UI;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FocusVolumeControl.AudioHelpers;

public class NameAndIconHelper
{
	public string GetProcessInfo(Process process)
	{
		//i know this is dumb, but its only used by the sound browser, not real prod code
		var blah = new ActiveAudioSessionWrapper();
		SetProcessInfo(process, blah);
		return blah.DisplayName;
	}

	public void SetProcessInfo(Process process, ActiveAudioSessionWrapper results)
	{
		try
		{
			//appx packages are installed from the windows store. eg, itunes
			var appx = AppxPackage.FromProcess(process);
			if (appx == null)
			{
				//using process.MainModule.FileVersionInfo sometimes throws permission exceptions
				//we get the file version info with a limited query flag to avoid that
				var fileVersionInfo = GetFileVersionInfo(process);

				//if the display name is already set, then it came from the display name of the audio session
				if (string.IsNullOrEmpty(results.DisplayName))
				{
					results.DisplayName = process.MainWindowTitle;

					if (string.IsNullOrEmpty(results.DisplayName))
					{
						results.DisplayName = fileVersionInfo?.FileDescription;
						if (string.IsNullOrEmpty(results.DisplayName))
						{
							results.DisplayName = process.ProcessName;
						}
					}
				}

				//for java apps (minecraft), the process will just have a java icon
				//and there's not just a file that you can get the real icon from
				//so you have to send some messages to the apps to get the icons.
				//but they will only be 32x32 (or smaller) so we only want to use this logic for java
				//because these will be lower resolution than the normal way of getting icons
				if (process.ProcessName == "javaw" || process.ProcessName == "java" || process.ProcessName == "dotnet")
				{
					var windowHandle = process.MainWindowHandle;
					var lazyIcon = () => JavaIconExtractor.GetWindowBigIconWithRetry(windowHandle);
					results.IconWrapper = new RawIcon(windowHandle.ToString(), lazyIcon);

				}
				else
				{
					results.IconWrapper = new NormalIcon(fileVersionInfo?.FileName);
				}
			}
			else
			{
				results.DisplayName = appx.DisplayName;
				results.IconWrapper = new AppxIcon(Path.Combine(appx.Path, appx.Logo));
			}
		}
		catch { }
		finally
		{
			//if anything threw an exception, set the display name to the process name, and just let the
			// icon/executable path be blank and the stream deck will just show the default icon
			if (string.IsNullOrEmpty(results.DisplayName))
			{
				results.DisplayName = process.ProcessName;
			}
		}
	}

	FileVersionInfo GetFileVersionInfo(Process process)
	{
		var path = GetExecutablePathWithPInvoke(process);
		if (!string.IsNullOrEmpty(path))
		{
			return FileVersionInfo.GetVersionInfo(path);
		}
		return null;
	}

	string GetExecutablePathWithPInvoke(Process process)
	{
		IntPtr processHandle = IntPtr.Zero;
		try
		{
			string pathToExe = string.Empty;

			if (process != null)
			{
				//use query limited information handle instead of process.handle to prevent permission errors
				processHandle = Native.OpenProcess(0x00001000, false, process.Id);

				var buffer = new StringBuilder(1024);
				var bufferSize = (uint)buffer.Capacity + 1;
				var success = Native.QueryFullProcessImageName(processHandle, 0, buffer, ref bufferSize);

				if (success)
				{
					return buffer.ToString();
				}
				else
				{
					var error = Marshal.GetLastWin32Error();
					Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error = {error} getting process name");
					return "";
				}
			}
		}
		catch
		{
		}
		finally
		{
			if(processHandle != IntPtr.Zero)
			{ 
				Native.CloseHandle(processHandle);
			}

		}
		return "";
	}

}
