using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DfuSe.Core.Windows
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct USB_DEVICE_DESCRIPTOR
    {
        public byte bLength;
        public byte bDescriptorType;
        public ushort bcdUSB;
        public byte bDeviceClass;
        public byte bDeviceSubClass;
        public byte bDeviceProtocol;
        public byte bMaxPacketSize0;
        public ushort idVendor;
        public ushort idProduct;
        public ushort bcdDevice;
        public byte iManufacturer;
        public byte iProduct;
        public byte iSerialNumber;
        public byte bNumConfigurations;
    }
}
