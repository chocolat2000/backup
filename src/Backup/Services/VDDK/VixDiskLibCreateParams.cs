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

public class VixDiskLibCreateParams : global::System.IDisposable {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal VixDiskLibCreateParams(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(VixDiskLibCreateParams obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  ~VixDiskLibCreateParams() {
    Dispose();
  }

  public virtual void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          VixDiskLibPINVOKE.delete_VixDiskLibCreateParams(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
      global::System.GC.SuppressFinalize(this);
    }
  }

  public VixDiskLibDiskType diskType {
    set {
      VixDiskLibPINVOKE.VixDiskLibCreateParams_diskType_set(swigCPtr, (int)value);
    } 
    get {
      VixDiskLibDiskType ret = (VixDiskLibDiskType)VixDiskLibPINVOKE.VixDiskLibCreateParams_diskType_get(swigCPtr);
      return ret;
    } 
  }

  public VixDiskLibAdapterType adapterType {
    set {
      VixDiskLibPINVOKE.VixDiskLibCreateParams_adapterType_set(swigCPtr, (int)value);
    } 
    get {
      VixDiskLibAdapterType ret = (VixDiskLibAdapterType)VixDiskLibPINVOKE.VixDiskLibCreateParams_adapterType_get(swigCPtr);
      return ret;
    } 
  }

  public ushort hwVersion {
    set {
      VixDiskLibPINVOKE.VixDiskLibCreateParams_hwVersion_set(swigCPtr, value);
    } 
    get {
      ushort ret = VixDiskLibPINVOKE.VixDiskLibCreateParams_hwVersion_get(swigCPtr);
      return ret;
    } 
  }

  public ulong capacity {
    set {
      VixDiskLibPINVOKE.VixDiskLibCreateParams_capacity_set(swigCPtr, value);
    } 
    get {
      ulong ret = VixDiskLibPINVOKE.VixDiskLibCreateParams_capacity_get(swigCPtr);
      return ret;
    } 
  }

  public VixDiskLibCreateParams() : this(VixDiskLibPINVOKE.new_VixDiskLibCreateParams(), true) {
  }

}

}
