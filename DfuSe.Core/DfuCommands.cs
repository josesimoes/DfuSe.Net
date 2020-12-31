namespace DfuSe.Core
{
    public enum DfuCommands : byte
    {
        DFU_DETACH = 0x00,
        DFU_DNLOAD = 0x01,
        DFU_UPLOAD = 0x02,
        DFU_GETSTATUS = 0x03,
        DFU_CLRSTATUS = 0x04,
        DFU_GETSTATE = 0x05,
        DFU_ABORT = 0x06,
    }
}
