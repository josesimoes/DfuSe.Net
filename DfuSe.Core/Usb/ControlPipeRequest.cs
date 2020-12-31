using System.Runtime.InteropServices;

namespace DfuSe.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ControlPipeRequest
    {
        // this is URB_FUNCTION_CLASS_INTERFACE
        public ushort Function;
        public uint Direction;
        public byte Request;
        public ushort Value;
        public ushort Index;
        public uint Length;
    }
}
