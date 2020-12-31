using System;
using System.IO;

namespace DfuSe.Core.Windows
{
    /// <summary>
    /// IO Control Codes
    /// Useful links:
    /// http://www.ioctls.net/
    /// http://msdn.microsoft.com/en-us/library/windows/hardware/ff543023(v=vs.85).aspx
    /// </summary>
    [Flags]
    public enum EIOControlCode : uint
    {
        // DFU
        GetNumberOfConfigurations = (EFileDevice.Unknown << 16) | (0x0800 << 2) | EMethod.Buffered | (0 << 14),
        GetConfigDescriptor = (EFileDevice.Unknown << 16) | (0x0801 << 2) | EMethod.Buffered | (0 << 14),
        GetDeviceDescriptor = (EFileDevice.Unknown << 16) | (0x0802 << 2) | EMethod.Buffered | (0 << 14),
        GetStringDescriptor = (EFileDevice.Unknown << 16) | (0x0803 << 2) | EMethod.Buffered | (0 << 14),
        VendorRequest = (EFileDevice.Unknown << 16) | (0x0805 << 2) | EMethod.Buffered | (0 << 14),
    }
}