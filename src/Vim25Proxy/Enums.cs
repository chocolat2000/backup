using System;
using System.Collections.Generic;
using System.Text;

namespace Vim25Proxy
{
    public enum VirtualMachinePowerState
    {
        poweredOff = Vim25Api.VirtualMachinePowerState.poweredOff,
        poweredOn = Vim25Api.VirtualMachinePowerState.poweredOn,
        suspended = Vim25Api.VirtualMachinePowerState.suspended
    }
}
