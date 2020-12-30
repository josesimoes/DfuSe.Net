using Microsoft.Win32.SafeHandles;
using System;

namespace DfuSe.Core
{
    public class StDfu
    {
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

        public void STDFU_Close(StDevice device)
        {
            m_pMgr.Close(device);
        }
    }
}
