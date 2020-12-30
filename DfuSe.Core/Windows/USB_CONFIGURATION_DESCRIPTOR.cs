using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DfuSe.Core.Windows
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class USB_CONFIGURATION_DESCRIPTOR
    {
        public byte bLength;
        public byte bDescriptorType;
        public ushort wTotalLength;
        public byte bNumInterfaces;
        public byte bConfigurationValue;
        public byte iConfiguration;
        public byte bmAttributes;
        public byte MaxPower;
    }
}
