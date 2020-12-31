using System.Runtime.InteropServices;

namespace DfuSe.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe public struct DfuStatus
    {
        public byte bStatus;
        public fixed byte bwPollTimeout[3];
        public byte bState;
        public byte iString;
    }
}
