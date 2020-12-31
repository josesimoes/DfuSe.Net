using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DfuSe.Core
{
    public class StDevicesManager
    {
        List<StDevice> m_OpenDevices = new List<StDevice>();

        public StDevice Open(
            string symbName,
            SafeFileHandle device,
            SafeFileHandle unplugEvent)
        {
            if (string.IsNullOrEmpty(symbName))
            {
                throw new ArgumentException($"'{nameof(symbName)}' cannot be null or empty", nameof(symbName));
            }

            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            var stDevice = new StDevice(symbName);

            var ret = stDevice.Open(unplugEvent);

            if (ret == StDeviceErrors.STDEVICE_NOERROR)
            {
                // OK our STDevice object was successfully created. Let's add it to our collection
                m_OpenDevices.Add(stDevice);
            }

            return stDevice;
        }

        public void Close(StDevice device)
        {
            if(m_OpenDevices.Contains(device))
            {
                device.Close();

                m_OpenDevices.Remove(device);
            }
        }

        internal T ControlPipeRequest<T>(StDevice device, ControlPipeRequest request)
        {
            if (m_OpenDevices.Contains(device))
            {
                return device.ControlPipeRequest<T>(request);


            }

            return default(T);
        }
    }
}
