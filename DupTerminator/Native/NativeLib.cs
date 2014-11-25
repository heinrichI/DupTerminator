using System;
using System.Diagnostics;

namespace DupTerminator.Native
{
	/// <summary>
	/// Interface to native library (library containing fast versions of
	/// several cryptographic functions).
	/// </summary>
	public static class NativeLib
	{
		private static bool m_bAllowNative = true;

		/// <summary>
		/// If this property is set to <c>true</c>, the native library is used.
		/// If it is <c>false</c>, all calls to functions in this class will fail.
		/// </summary>
		public static bool AllowNative
		{
			get { return m_bAllowNative; }
			set { m_bAllowNative = value; }
		}


		private static bool? m_bIsUnix = null;
		public static bool IsUnix()
		{
			if(m_bIsUnix.HasValue) return m_bIsUnix.Value;

			PlatformID p = GetPlatformID();

			// Mono defines Unix as 128 in early .NET versions
			m_bIsUnix = ((p == PlatformID.Unix) || (p == PlatformID.MacOSX) ||
				((int)p == 128));

			return m_bIsUnix.Value;
		}

		private static PlatformID? m_platID = null;
		public static PlatformID GetPlatformID()
		{
			if(m_platID.HasValue) return m_platID.Value;

			m_platID = Environment.OSVersion.Platform;

            // Mono returns PlatformID.Unix on Mac OS X, workaround this
			if(m_platID.Value == PlatformID.Unix)
			{
                if ((RunConsoleApp("uname", null) ?? string.Empty).Trim().Equals("Darwin", StringComparison.OrdinalIgnoreCase))
					m_platID = PlatformID.MacOSX;
			}

			return m_platID.Value;
		}

		public static string RunConsoleApp(string strAppPath, string strParams)
		{
			return RunConsoleApp(strAppPath, strParams, null);
		}

		public static string RunConsoleApp(string strAppPath, string strParams,
			string strStdInput)
		{
			if(strAppPath == null) throw new ArgumentNullException("strAppPath");
			if(strAppPath.Length == 0) throw new ArgumentException("strAppPath");

			try
			{
				ProcessStartInfo psi = new ProcessStartInfo();

				psi.CreateNoWindow = true;
				psi.FileName = strAppPath;
				psi.WindowStyle = ProcessWindowStyle.Hidden;
				psi.UseShellExecute = false;
				psi.RedirectStandardOutput = true;

				if(strStdInput != null) psi.RedirectStandardInput = true;

				if(!string.IsNullOrEmpty(strParams)) psi.Arguments = strParams;

				Process p = Process.Start(psi);

				if(strStdInput != null)
				{
					p.StandardInput.Write(strStdInput);
					p.StandardInput.Close();
				}

				string strOutput = p.StandardOutput.ReadToEnd();
				p.WaitForExit();

				return strOutput;
			}
			catch(Exception) { Debug.Assert(false); }

			return null;
		}
	}
}
