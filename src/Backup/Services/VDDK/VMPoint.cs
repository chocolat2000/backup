//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (http://www.swig.org).
// Version 3.0.12
//
// Do not make changes to this file unless you know what you are doing--modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------

namespace Backup.Services.VDDK {

public class VMPoint : global::System.IDisposable {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal VMPoint(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(VMPoint obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  ~VMPoint() {
    Dispose();
  }

  public virtual void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          VixDiskLibPINVOKE.delete_VMPoint(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
      global::System.GC.SuppressFinalize(this);
    }
  }

  public int x {
    set {
      VixDiskLibPINVOKE.VMPoint_x_set(swigCPtr, value);
    } 
    get {
      int ret = VixDiskLibPINVOKE.VMPoint_x_get(swigCPtr);
      return ret;
    } 
  }

  public int y {
    set {
      VixDiskLibPINVOKE.VMPoint_y_set(swigCPtr, value);
    } 
    get {
      int ret = VixDiskLibPINVOKE.VMPoint_y_get(swigCPtr);
      return ret;
    } 
  }

  public VMPoint() : this(VixDiskLibPINVOKE.new_VMPoint(), true) {
  }

}

}