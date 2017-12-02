#include <stdarg.h>
#include <stdio.h>

#ifdef _WIN32
#  define EXPORTIT __declspec( dllexport )
#else
#  define EXPORTIT __attribute__((visibility("default")))
#endif

typedef void (*callback) (const char *format, va_list args);

void listifyAndCall(callback cb, const char *format, ...)
{
    va_list argsList;

    va_start(argsList, format);
    cb(format, argsList);
    va_end(argsList);
}

EXPORTIT void triggerCallback(callback cb) {
    listifyAndCall(cb, "hello %s, the answer is %d", "world", 42);
}
