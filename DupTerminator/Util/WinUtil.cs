using System;
using NativeLib = DupTerminator.Native.NativeLib;

namespace DupTerminator.Util
{
	public static class WinUtil
	{
		private const int ERROR_ACCESS_DENIED = 5;

		private static bool m_bIsWindows9x = false;
		private static bool m_bIsWindows2000 = false;
		private static bool m_bIsWindowsXP = false;
		private static bool m_bIsAtLeastWindows2000 = false;
		private static bool m_bIsAtLeastWindowsVista = false;
		private static bool m_bIsAtLeastWindows7 = false;

		//private static string m_strExePath = null;

		//private static ulong m_uFrameworkVersion = 0;

		public static bool IsWindows9x
		{
			get { return m_bIsWindows9x; }
		}

		public static bool IsWindows2000
		{
			get { return m_bIsWindows2000; }
		}

		public static bool IsWindowsXP
		{
			get { return m_bIsWindowsXP; }
		}

		public static bool IsAtLeastWindows2000
		{
			get { return m_bIsAtLeastWindows2000; }
		}

		public static bool IsAtLeastWindowsVista
		{
			get { return m_bIsAtLeastWindowsVista; }
		}

		public static bool IsAtLeastWindows7
		{
			get { return m_bIsAtLeastWindows7; }
		}

		static WinUtil()
		{
			OperatingSystem os = Environment.OSVersion;
			System.Version v = os.Version;

			m_bIsWindows9x = (os.Platform == PlatformID.Win32Windows);
			m_bIsWindows2000 = ((v.Major == 5) && (v.Minor == 0));
			m_bIsWindowsXP = ((v.Major == 5) && (v.Minor == 1));

			m_bIsAtLeastWindows2000 = (v.Major >= 5);
			m_bIsAtLeastWindowsVista = (v.Major >= 6);
			m_bIsAtLeastWindows7 = ((v.Major >= 7) || ((v.Major == 6) && (v.Minor >= 1)));
		}

		public static string GetOSStr()
		{
			if(NativeLib.IsUnix()) return "Unix";
			return "Windows";
		}


	}
}
