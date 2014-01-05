using System;
using System.Windows;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Sheva.Internal
{
    internal sealed class NativeTypes
    {
        [StructLayout(LayoutKind.Sequential)]
        public sealed class SECURITY_ATTRIBUTES : IDisposable
        {
            public Int32 nLength;
            public IntPtr lpSecurityDescriptor;
            public Boolean bInheritHandle;
            public SECURITY_ATTRIBUTES()
            {
                this.nLength = Marshal.SizeOf(typeof(NativeTypes.SECURITY_ATTRIBUTES));
            }

            public void Dispose()
            {
                DisposeCore();
                GC.SuppressFinalize(this);
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            internal void DisposeCore()
            {
                if (this.lpSecurityDescriptor != IntPtr.Zero)
                {
                    UnsafeNativeMethods.LocalFree(this.lpSecurityDescriptor);
                    this.lpSecurityDescriptor = IntPtr.Zero;
                }
            }

            ~SECURITY_ATTRIBUTES()
            {
                this.DisposeCore();
            }
        }
    }
}
