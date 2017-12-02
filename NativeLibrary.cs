namespace VaListInterop
{
    using System;

    /// <summary>
    /// The class that declares the exported declarations of the library
    /// </summary>
    public class NativeLibrary
    {
        public delegate void Callback(string format, IntPtr args);
        public delegate void TriggerCallback(Callback cb);
    }
}