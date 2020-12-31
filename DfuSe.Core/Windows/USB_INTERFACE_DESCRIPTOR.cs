using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DfuSe.Core.Windows
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class USB_INTERFACE_DESCRIPTOR
    {
        public byte bLength;
        public byte bDescriptorType;
        public byte bInterfaceNumber;
        public byte bAlternateSetting;
        public byte bNumEndpoints;
        public byte bInterfaceClass;
        public byte bInterfaceSubClass;
        public byte bInterfaceProtocol;
        public byte iInterface;
    }
}
