using System.Runtime.InteropServices;

namespace VaListInterop
{
    using System;

    /// <summary>
    /// The class that declares the exported declarations of the library
    /// </summary>
    public class NativeLibrary
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Callback(string format, IntPtr args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void TriggerCallback(Callback cb);
    }
}