using System.Runtime.InteropServices;

namespace DfuSe.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class DfuFunctionalDescriptor
    {
        public const int DefaultSize = 9;
        public const int Type = 0x21;

        public byte bLength;
        public byte bDescriptorType;
        public byte bmAttributes;
        public ushort wDetachTimeOut;
        public ushort wTransfertSize;
        public ushort bcdDFUVersion;
    }
}
