using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FocusVolumeControl.AudioHelpers;

public sealed class AppxPackage
{
	private AppxPackage()
	{
	}

	public string Path { get; private set; }
	public string Logo { get; private set; }
	public string DisplayName { get; private set; }

	public static AppxPackage FromProcess(Process process)
	{
		try
		{
			return FromProcess(process.Handle);
		}
		catch
		{
			return null;
		}
	}

	public static AppxPackage FromProcess(int processId)
	{
		const int QueryLimitedInformation = 0x1000;
		IntPtr hProcess = OpenProcess(QueryLimitedInformation, false, processId);
		try
		{
			return FromProcess(hProcess);
		}
		finally
		{
			if (hProcess != IntPtr.Zero)
			{
				CloseHandle(hProcess);
			}
		}
	}

	static AppxPackage FromProcess(IntPtr hProcess)
	{
		if (hProcess == IntPtr.Zero)
		{
			return null;
		}

		int len = 0;
		GetPackageFullName(hProcess, ref len, null);
		if (len == 0)
		{
			return null;
		}

		var sb = new StringBuilder(len);
		string fullName = GetPackageFullName(hProcess, ref len, sb) == 0 ? sb.ToString() : null;
		if (string.IsNullOrEmpty(fullName)) // not an AppX
		{
			return null;
		}

		var package = QueryPackageInfo(fullName, PackageConstants.PACKAGE_FILTER_HEAD).First();

		return package;
	}

	private static IEnumerable<AppxPackage> QueryPackageInfo(string fullName, PackageConstants flags)
	{
		IntPtr infoRef;
		OpenPackageInfoByFullName(fullName, 0, out infoRef);
		if (infoRef != IntPtr.Zero)
		{
			IntPtr infoBuffer = IntPtr.Zero;
			try
			{
				int len = 0;
				int count;
				GetPackageInfo(infoRef, flags, ref len, IntPtr.Zero, out count);
				if (len > 0)
				{
					var factory = (IAppxFactory)new AppxFactory();
					infoBuffer = Marshal.AllocHGlobal(len);
					int res = GetPackageInfo(infoRef, flags, ref len, infoBuffer, out count);
					for (int i = 0; i < count; i++)
					{
						var info = (PACKAGE_INFO)Marshal.PtrToStructure(infoBuffer + i * Marshal.SizeOf(typeof(PACKAGE_INFO)), typeof(PACKAGE_INFO));
						var package = new AppxPackage();
						package.Path = Marshal.PtrToStringUni(info.path);

						// read manifest
						string manifestPath = System.IO.Path.Combine(package.Path, "AppXManifest.xml");
						const int STGM_SHARE_DENY_NONE = 0x40;

						SHCreateStreamOnFileEx(manifestPath, STGM_SHARE_DENY_NONE, 0, false, IntPtr.Zero, out var strm);
						if (strm != null)
						{
							var reader = factory.CreateManifestReader(strm);
							var properties = reader.GetProperties();

							properties.GetStringValue("DisplayName", out var displayName);
							package.DisplayName = displayName;

							properties.GetStringValue("Logo", out var logo);
							package.Logo = logo;

							/*
							var apps = reader.GetApplications();
							while (apps.GetHasCurrent())
							{
								var app = apps.GetCurrent();
								var appx = new AppxApp(app);
								appx.Description = GetStringValue(app, "Description");
								appx.DisplayName = GetStringValue(app, "DisplayName");
								appx.EntryPoint = GetStringValue(app, "EntryPoint");
								appx.Executable = GetStringValue(app, "Executable");
								appx.Id = GetStringValue(app, "Id");
								appx.Logo = GetStringValue(app, "Logo");
								appx.SmallLogo = GetStringValue(app, "SmallLogo");
								appx.StartPage = GetStringValue(app, "StartPage");
								appx.Square150x150Logo = GetStringValue(app, "Square150x150Logo");
								appx.Square30x30Logo = GetStringValue(app, "Square30x30Logo");
								appx.BackgroundColor = GetStringValue(app, "BackgroundColor");
								appx.ForegroundText = GetStringValue(app, "ForegroundText");
								appx.WideLogo = GetStringValue(app, "WideLogo");
								appx.Wide310x310Logo = GetStringValue(app, "Wide310x310Logo");
								appx.ShortName = GetStringValue(app, "ShortName");
								appx.Square310x310Logo = GetStringValue(app, "Square310x310Logo");
								appx.Square70x70Logo = GetStringValue(app, "Square70x70Logo");
								appx.MinWidth = GetStringValue(app, "MinWidth");
								package._apps.Add(appx);
								apps.MoveNext();
							}
							*/
							Marshal.ReleaseComObject(strm);
						}
						yield return package;
					}
					Marshal.ReleaseComObject(factory);
				}
			}
			finally
			{
				if (infoBuffer != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(infoBuffer);
				}
				ClosePackageInfo(infoRef);
			}
		}
	}


	[Guid("5842a140-ff9f-4166-8f5c-62f5b7b0c781"), ComImport]
	private class AppxFactory
	{
	}

	[Guid("BEB94909-E451-438B-B5A7-D79E767B75D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAppxFactory
	{
		void _VtblGap0_2(); // skip 2 methods
		IAppxManifestReader CreateManifestReader(IStream inputStream);
	}

	[Guid("4E1BD148-55A0-4480-A3D1-15544710637C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAppxManifestReader
	{
		void _VtblGap0_1(); // skip 1 method
		IAppxManifestProperties GetProperties();
		void _VtblGap1_5(); // skip 5 methods
		IAppxManifestApplicationsEnumerator GetApplications();
	}

	[Guid("9EB8A55A-F04B-4D0D-808D-686185D4847A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAppxManifestApplicationsEnumerator
	{
		IAppxManifestApplication GetCurrent();
		bool GetHasCurrent();
		bool MoveNext();
	}

	[Guid("5DA89BF4-3773-46BE-B650-7E744863B7E8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IAppxManifestApplication
	{
		[PreserveSig]
		int GetStringValue([MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] out string vaue);
	}

	[Guid("03FAF64D-F26F-4B2C-AAF7-8FE7789B8BCA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAppxManifestProperties
	{
		[PreserveSig]
		int GetBoolValue([MarshalAs(UnmanagedType.LPWStr)] string name, out bool value);
		[PreserveSig]
		int GetStringValue([MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] out string vaue);
	}

	[DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
	private static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);

	[DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
	private static extern int SHCreateStreamOnFileEx(string fileName, int grfMode, int attributes, bool create, IntPtr reserved, out IStream stream);

	[DllImport("user32.dll")]
	private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

	[DllImport("kernel32.dll")]
	private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

	[DllImport("kernel32.dll")]
	private static extern bool CloseHandle(IntPtr hObject);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern int OpenPackageInfoByFullName(string packageFullName, int reserved, out IntPtr packageInfoReference);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetPackageInfo(IntPtr packageInfoReference, PackageConstants flags, ref int bufferLength, IntPtr buffer, out int count);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern int ClosePackageInfo(IntPtr packageInfoReference);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetPackageFullName(IntPtr hProcess, ref int packageFullNameLength, StringBuilder packageFullName);

	[Flags]
	private enum PackageConstants
	{
		PACKAGE_FILTER_ALL_LOADED = 0x00000000,
		PACKAGE_PROPERTY_FRAMEWORK = 0x00000001,
		PACKAGE_PROPERTY_RESOURCE = 0x00000002,
		PACKAGE_PROPERTY_BUNDLE = 0x00000004,
		PACKAGE_FILTER_HEAD = 0x00000010,
		PACKAGE_FILTER_DIRECT = 0x00000020,
		PACKAGE_FILTER_RESOURCE = 0x00000040,
		PACKAGE_FILTER_BUNDLE = 0x00000080,
		PACKAGE_INFORMATION_BASIC = 0x00000000,
		PACKAGE_INFORMATION_FULL = 0x00000100,
		PACKAGE_PROPERTY_DEVELOPMENT_MODE = 0x00010000,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct PACKAGE_INFO
	{
		public int reserved;
		public int flags;
		public IntPtr path;
		public IntPtr packageFullName;
		public IntPtr packageFamilyName;
		public PACKAGE_ID packageId;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct PACKAGE_ID
	{
		public int reserved;
		public AppxPackageArchitecture processorArchitecture;
		public ushort VersionRevision;
		public ushort VersionBuild;
		public ushort VersionMinor;
		public ushort VersionMajor;
		public IntPtr name;
		public IntPtr publisher;
		public IntPtr resourceId;
		public IntPtr publisherId;
	}
}

public enum AppxPackageArchitecture
{
	x86 = 0,
	Arm = 5,
	x64 = 9,
	Neutral = 11,
	Arm64 = 12
}
