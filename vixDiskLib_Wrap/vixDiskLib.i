%module VixDiskLib
%typemap(cstype) void (VixDiskLibGenericLogFunc)(const char *fmt, va_list args) "VixDiskLibGenericLogFuncDelegate";
%typemap(imtype) void (VixDiskLibGenericLogFunc)(const char *fmt, va_list args) "VixDiskLibGenericLogFuncDelegate";

%{
#include "vixDiskLib.h"
%}
#define _MSC_VER
%include <typemaps.i>
%include <windows.i>

%include "arrays_csharp.i"

%apply unsigned char FIXED[] { uint8 *readBuffer, const uint8 *writeBuffer, char *keys, char *buf }
%csmethodmodifiers VixDiskLib_Read "public unsafe";
%csmethodmodifiers VixDiskLib_ReadAsync "public unsafe";
%csmethodmodifiers VixDiskLib_Write "public unsafe";
%csmethodmodifiers VixDiskLib_WriteAsync "public unsafe";
%csmethodmodifiers VixDiskLib_GetMetadataKeys "public unsafe";
%csmethodmodifiers VixDiskLib_ReadMetadata "public unsafe";

%apply unsigned long *OUTPUT { size_t *requiredLen };

%typemap(ctype, out="void *")
    VixDiskLibConnection,
	const VixDiskLibConnection,
	VixDiskLibConnection *
	VixDiskLibHandle,
	VixDiskLibHandle *
	"void *"
%typemap(imtype, out="global::System.IntPtr") VixDiskLibConnection *, VixDiskLibHandle* "out global::System.IntPtr"
%typemap(imtype, out="global::System.IntPtr") VixDiskLibConnection, const VixDiskLibConnection, VixDiskLibHandle "global::System.IntPtr"
%typemap(cstype, out="$csclassname") VixDiskLibConnection *, VixDiskLibHandle * "out global::System.IntPtr"
%typemap(cstype, out="$csclassname") VixDiskLibConnection, const VixDiskLibConnection, VixDiskLibHandle "global::System.IntPtr"
%typemap(csin) VixDiskLibConnection *, VixDiskLibHandle * "out $csinput"
%typemap(csin) VixDiskLibConnection, const VixDiskLibConnection, VixDiskLibHandle "$csinput"

%typemap(in) VixDiskLibConnection, const VixDiskLibConnection, VixDiskLibConnection *, VixDiskLibHandle, VixDiskLibHandle *
%{ $1 = ($1_ltype)$input; %}


%include "vm_basic_types.h"
%include "vixDiskLib.h"


