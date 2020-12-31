using DfuSe.Core.Usb;
using DfuSe.Core.Windows;
using Microsoft.Win32.SafeHandles;
using System;

namespace DfuSe.Core
{
    public class StDfu
    {
        private const ushort URB_FUNCTION_CLASS_INTERFACE = 0x001B;

        StDevicesManager m_pMgr = new StDevicesManager();

        public StDevice STDFU_Open(string devicePath, SafeFileHandle device)
        {
            return STDevice_Open(devicePath, device, null);
        }

        private StDevice STDevice_Open(
            string devicePath,
            SafeFileHandle device,
            SafeFileHandle unplugEvent)
        {
            if (string.IsNullOrEmpty(devicePath))
            {
                throw new ArgumentException($"'{nameof(devicePath)}' cannot be null or empty", nameof(devicePath));
            }

            string symbName;

            symbName = devicePath;

            return m_pMgr.Open(
                symbName,
                device,
                unplugEvent);
        }

        private T STDevice_ControlPipeRequest<T>(
            StDevice device,
            ControlPipeRequest request)
        {

            return m_pMgr.ControlPipeRequest<T>(
                device,
                request);
        }

        public void STDFU_Close(StDevice device)
        {
            m_pMgr.Close(device);
        }

        public DfuStatus STDFU_Getstatus(StDevice device)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            ControlPipeRequest request = new ControlPipeRequest();

            request.Function = URB_FUNCTION_CLASS_INTERFACE;
            request.Direction = (byte)RequestDirection.In;
            request.Request = (byte)DfuCommands.DFU_GETSTATUS;
            request.Value = 0;
            request.Index = 0;
            request.Length = 6;

            return STDevice_ControlPipeRequest<DfuStatus>(device, request);

        }
    }
}
