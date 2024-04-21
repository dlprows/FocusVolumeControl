using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace FocusVolumeControl.UI
{
	internal class JavaIconExtractor
	{
		const int WM_GETICON = 0x7F;
		const int WM_QUERYDRAGICON = 0x0037;
		//const int ICON_SMALL = 0; //(16x16)
		const int ICON_BIG = 1; //(32x32)
		const int SMTO_ABORTIFHUNG = 0x3;
		const int GCL_HICON = -14;

		[DllImport("User32.dll")]
		static extern int SendMessageTimeout(IntPtr hWnd, int uMsg, int wParam, int lParam, int fuFlags, int uTimeout, out int lpdwResult);
		[DllImport("User32.dll")]
		static extern int GetClassLong(IntPtr hWnd, int index);

		[DllImport("user32.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
		public static extern int DestroyIcon(IntPtr hIcon);

		public static Bitmap? GetWindowBigIconWithRetry(IntPtr hWnd)
		{
			var retry = 5;
			var icon = GetWindowBigIcon(hWnd);

			while(icon == null || retry > 0)
			{
				Thread.Sleep(100);
				icon = GetWindowBigIcon(hWnd);
				retry--;
			}

			return icon;

		}

		/// <summary>
		/// Retrieves a big icon (32*32) of a window
		/// </summary>
		/// <param name="hWnd"></param>
		/// <returns></returns>
		public static Bitmap? GetWindowBigIcon(IntPtr hWnd)
		{
			IntPtr hIcon = IntPtr.Zero;
			try
			{
				int result;
				SendMessageTimeout(hWnd, WM_GETICON, ICON_BIG, //big icon size
					0, SMTO_ABORTIFHUNG, 1000, out result);

				hIcon = new IntPtr(result);
				if (hIcon == IntPtr.Zero) //some applications don't respond to sendmessage, we have to use GetClassLong in that case
				{
					result = GetClassLong(hWnd, GCL_HICON); //big icon size
					hIcon = new IntPtr(result);
				}

				if (hIcon == IntPtr.Zero)
				{
					SendMessageTimeout(hWnd, WM_QUERYDRAGICON, 0, 0, SMTO_ABORTIFHUNG, 1000, out result);
					hIcon = new IntPtr(result);
				}

				if (hIcon == IntPtr.Zero)
				{
					return null;
				}
				else
				{
					using var tmp = (Icon)Icon.FromHandle(hIcon).Clone();
					return tmp.ToBitmap();
				}
			}
			catch (Exception)
			{
			}
			finally
			{
				if(hIcon != IntPtr.Zero)
				{
					DestroyIcon(hIcon);
				}
			}
			return null;
		}

	}
}

#nullable restore
