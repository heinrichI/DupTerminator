using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SevenZipExtractor
{
    internal class SevenZipHandle : IDisposable
    {
        private SafeLibraryHandle _sevenZipSafeHandle;

        public SevenZipHandle(string sevenZipLibPath)
        {
            this._sevenZipSafeHandle = Kernel32Dll.LoadLibrary(sevenZipLibPath);

            if (this._sevenZipSafeHandle.IsInvalid)
            {
                throw new Win32Exception();
            }

            IntPtr functionPtr = Kernel32Dll.GetProcAddress(this._sevenZipSafeHandle, "GetHandlerProperty");
            
            // Not valid dll
            if (functionPtr == IntPtr.Zero)
            {
                this._sevenZipSafeHandle.Close();
                throw new ArgumentException();
            }
        }

        ~SevenZipHandle()
        {
            this.Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if ((this._sevenZipSafeHandle != null) && !this._sevenZipSafeHandle.IsClosed)
            {
                this._sevenZipSafeHandle.Close();
            }

            this._sevenZipSafeHandle = null;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IInArchive CreateInArchive(Guid classId)
        {
            if (this._sevenZipSafeHandle == null)
            {
                throw new ObjectDisposedException("SevenZipHandle");
            }

            IntPtr procAddress = Kernel32Dll.GetProcAddress(this._sevenZipSafeHandle, "CreateObject");
            CreateObjectDelegate createObject = (CreateObjectDelegate) Marshal.GetDelegateForFunctionPointer(procAddress, typeof (CreateObjectDelegate));

            object result;
            Guid interfaceId = typeof (IInArchive).GUID;
            createObject(ref classId, ref interfaceId, out result);

            return result as IInArchive;
        }
    }
}