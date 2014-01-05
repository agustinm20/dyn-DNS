using System;
using System.Runtime.InteropServices;

namespace Sheva.Internal
{
     internal static class NativeMethods
    {
         [return: MarshalAs(UnmanagedType.Bool)]
         [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
         public static extern Boolean ConvertStringSecurityDescriptorToSecurityDescriptor(String StringSecurityDescriptor, UInt32 StringSDRevision, ref IntPtr SecurityDescriptor, IntPtr SecurityDescriptorSize);
    }
}
