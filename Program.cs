namespace VaListInterop
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    public class Program
    {
        public static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                RunLinux();
            }
            else
            {
                Console.Error.WriteLine("Platform not supported yet");
            }
        }

        public static void RunLinux()
        {
            string arch;
            NativeLibrary.Callback callback;
            // Detect the process architecture
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                    arch = "x86";
                    callback = LinuxX86Callback;
                    break;
                case Architecture.X64:
                    arch = "x64";
                    callback = LinuxX64Callback;
                    break;
                default:
                    throw new PlatformNotSupportedException("Only x86 and x64 are supported at the moment");
            }
            Console.WriteLine(arch);

            // Load the correct library
            var handle = LinuxInterop.dlopen(Path.Combine(Environment.CurrentDirectory, "nativeLibrary", "build", $"libnativeLibrary.{arch}.so"), LinuxInterop.RTLD_LAZY);
            if (handle == IntPtr.Zero)
            {
                Console.Error.WriteLine("Failed to load native library");
                throw new Exception(LinuxInterop.dlerror());
            }
            
            try
            {
                // Locate the "triggerCallback" function. This function just calls the callback with some parameters.
                var procAddress = LinuxInterop.dlsym(handle, "triggerCallback");
                if (procAddress == IntPtr.Zero)
                {
                    Console.Error.WriteLine("Failed to locate the triggerCallback function.");
                    throw new Exception(LinuxInterop.dlerror());
                }

                var triggerCallback = Marshal.GetDelegateForFunctionPointer<NativeLibrary.TriggerCallback>(procAddress);

                triggerCallback(callback);
            }
            finally
            {
                LinuxInterop.dlclose(handle);
            }
        }

        public static void LinuxX86Callback(string format, IntPtr args)
        {
            // This implementation is pretty straightforward. The IntPtr can be passed to the functions and can be reused.
            int byteLength = LinuxInterop.vsnprintf(IntPtr.Zero, UIntPtr.Zero, format, args) + 1;
            var utf8Buffer = Marshal.AllocHGlobal(byteLength);
            try
            {
                LinuxInterop.vsprintf(utf8Buffer, format, args);
                Console.WriteLine(Utf8ToString(utf8Buffer));
            }
            finally
            {
                Marshal.FreeHGlobal(utf8Buffer);
            }
        }

        public static void LinuxX64Callback(string format, IntPtr args)
        {
            // The args pointer cannot be reused between two calls. We need to make a copy of the underlying structure.
            var listStructure = Marshal.PtrToStructure<VaListLinuxX64>(args);
            IntPtr listPointer = Marshal.AllocHGlobal(Marshal.SizeOf(listStructure));
            int byteLength;
            try
            {
                Marshal.StructureToPtr(listStructure, listPointer, false);
                byteLength = LinuxInterop.vsnprintf(IntPtr.Zero, UIntPtr.Zero, format, listPointer) + 1;
            }
            finally
            {
                Marshal.FreeHGlobal(listPointer);
            }
            var utf8Buffer = Marshal.AllocHGlobal(byteLength);
            try
            {
                listPointer = Marshal.AllocHGlobal(Marshal.SizeOf(listStructure));
                try
                {
                    Marshal.StructureToPtr(listStructure, listPointer, false);
                    LinuxInterop.vsprintf(utf8Buffer, format, listPointer);
                    Console.WriteLine(Utf8ToString(utf8Buffer));
                }
                finally
                {
                    Marshal.FreeHGlobal(listPointer);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(utf8Buffer);
            }
        }

        public static string Utf8ToString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            var length = 0;

            while (Marshal.ReadByte(ptr, length) != 0)
            {
                length++;
            }

            byte[] buffer = new byte[length];
            Marshal.Copy(ptr, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }
    }
}
