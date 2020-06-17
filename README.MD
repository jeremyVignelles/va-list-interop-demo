# va_list interop in .net, cross-platform exploration project

## Motivation
Sometimes, you need to use a native library.
Sometimes, you want to support any platform.
Sometimes, you need to take/pass a variable arguments list, because that's how printf() works.
Sometimes, you want to do all of that... in .net... And you feel like an explorer in the middle of the Amazonian forest.

This is what happened to me while I was looking to implement a specific interop method for [Vlc.DotNet](https://github.com/ZeBobo5/Vlc.DotNet).

`libvlc_log_set` is a method in libvlc that registers a log callback.
The callback is declared as follows:

```
typedef void (*libvlc_log_cb) (void *data, int level, const libvlc_log_t *ctx, const char *fmt, va_list args)
```

For those who wonders what this awful syntax means, here is a translation:

> I hereby declare that `libvlc_log_cb` is an alias for a function pointer.
> This function takes 5 parameters and returns void.

There is something "funny" here :
The last two parameters are a format string and a variable arguments list.

This is the same as what [vprintf](http://www.cplusplus.com/reference/cstdio/vprintf/) expects.

In other words, `libvlc_log_set` stores your callback in memory, and calls it whenever you need it. If you want to print something to the console, you are responsible of calling `vprintf` with the last two arguments, and everything is fine. That's quite easy if you are in C/C++.

In .net, that's another story...

For reference, [we asked for something in the dotnet/runtime repo](https://github.com/dotnet/runtime/issues/9316), but it didn't went far because we lacked a formal proposal.

This repository is here to share my findings.

## The objective

`va_list` reading seems hard to do in a cross-platform way.
Each platforms has its own implementation and I'm not sure it would work.

The idea will be to be able to pass back the `va_list` to native `vprintf` family functions.

```
[native] someFunction -> [.net] yourCallback -> [native]vprintf
```

This project aims at providing sample native library that could call our .net callback. Our .net callback wants to receive the formatted message in an UTF-8 string.

All started with [this answer on stackoverflow](https://stackoverflow.com/questions/6694612/c-sharp-p-invoke-varargs-delegate-callback/37629480#37629480)

I saw this was kind of possible : the provided code indeed works on Windows.
But it didn't work on the new .net core for linux x64 nor does it work in mono (linux x64 too).

## Disclaimer
These findings are based on my observations and shall not be considered as a universal source of truth. In other words, I cannot be held responsible if that code doesn't work for your case.
However, if you find a case where it doesn't work, feel free to modify this code and make a PR.

## Windows (x86 & x64)
The `va_list` is a pointer (char*, see vadefs.h), that can be stored as-is in a `IntPtr`.
You can pass this pointer to the native methods.

Beware, while doing this project, I ran into a bugs of the .net framework : [RuntimeInformation.ProcessArchitecture returns invalid results](https://github.com/dotnet/corefx/issues/25267).
.net core works just fine.

## Linux x64
Source : https://www.uclibc.org/docs/psABI-x86_64.pdf at page 52 suggests that va_list is a pointer to a structure with 4 fields (2 unsigned ints and 2 pointers)

```
typedef struct {
   unsigned int gp_offset;
   unsigned int fp_offset;
   void *overflow_arg_area;
   void *reg_save_area;
} va_list[1];
```

Based on my experimentation, you will need to copy the structure before calling the function.

## macOS x64
Works mostly like Linux, tested with .NET Core 2.

## Linux x86
Works mostly like Windows, an IntPtr is enough.

## Linux ARM
Source : http://infocenter.arm.com/help/topic/com.arm.doc.ihi0042f/IHI0042F_aapcs.pdf at page 27 has the declaration of va_list for the ARM architecture:

```
struct __va_list
{
   void *__ap;
}
```

TODO: Implement this platform in the demo

## Other platforms
Contributions are welcome!

## How to build this project
Build the C source code

With cmake on Linux/macOS:

```
mkdir nativeLibrary/build
cd nativeLibrary/build
cmake ..
make
```
(Come back to the repository root)

Or with Visual studio for windows (With CMake tools installed):
- Open the nativeLibrary folder in Visual Studio (not as a project).
- Select "x86-Debug" from the drop down list at the top, build. Again with "x64-Debug"

Build the dotnet project:
- With Visual Studio, build the va-list-interop project. You can change the targetFramework in the csproj file, as well as the "Prefer 32 bits" flag.
- With the `dotnet` CLI on Windows/macOS/Linux:
  `dotnet build va-list-interop.csproj --framework netcoreapp2.0`

Run the project from the repository root. On ubuntu, you will need to install `libc6-dev` if it's not already present (to be able to call `libdl` and `vsprintf`).
- On visual studio, just run the project.
- With the `dotnet` CLI on Windows/macOS/Linux:
  `dotnet run --project va-list-interop.csproj --framework netcoreapp2.0`
- With the `mono` command line:
  `mono bin/Debug/va-list-interop.mono.exe`

## Limitations
You need to use the `va_list` argument inside the callback before the method returns. Otherwise, the `va_list` will be released.

Currently supported platforms are: 
* Windows with .NET Framework, 
* Windows with .NET Core (x86, x64),
* Linux with .NET Core (x64),
* Linux with Mono (x86, x64),
* macOS with .NET Core (x64).
