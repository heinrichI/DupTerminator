using System;
using System.Runtime.InteropServices;

namespace DupTerminator.Native
{
	internal static partial class NativeMethods
	{
        internal const int MAX_PATH = 260;

        internal const int LVM_FIRST = 0x1000;
        internal const int LVM_ENSUREVISIBLE = LVM_FIRST + 19;
        internal const int LVM_SCROLL = LVM_FIRST + 20;

        internal const int HDI_FORMAT = 0x0004;
        internal const int HDF_SORTUP = 0x0400;
        internal const int HDF_SORTDOWN = 0x0200;
        internal const int LVM_GETHEADER = LVM_FIRST + 31;
        internal const int HDM_FIRST = 0x1200;
        internal const int HDM_GETITEMA = HDM_FIRST + 3;
        internal const int HDM_GETITEMW = HDM_FIRST + 11;
        internal const int HDM_SETITEMA = HDM_FIRST + 4;
        internal const int HDM_SETITEMW = HDM_FIRST + 12;

        [StructLayout(LayoutKind.Sequential)]
        internal struct HDITEM
        {
            public UInt32 mask;
            public Int32 cxy;

            [MarshalAs(UnmanagedType.LPTStr)]
            public string pszText;

            public IntPtr hbm;
            public Int32 cchTextMax;
            public Int32 fmt;
            public IntPtr lParam;
            public Int32 iImage;
            public Int32 iOrder;
        }

		[DllImport("User32.dll")]
		internal static extern IntPtr SendMessage(IntPtr hWnd, int nMsg,
			IntPtr wParam, IntPtr lParam);

		[DllImport("User32.dll", EntryPoint = "SendMessage")]
		internal static extern IntPtr SendMessageHDItem(IntPtr hWnd, int nMsg,
			IntPtr wParam, ref HDITEM hdItem);
	}
}
