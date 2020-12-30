using DfuSe.Core;
using DfuSe.Core.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DfuSeUnitTests
{
    [TestClass]
    public class StDeviceTests
    {
        [TestMethod]
        public void TestOpen()
        {
            const string deviceID = @"\\?\usb#vid_0483&pid_df11#00000008ffff#{a5dcbf10-6530-11d2-901f-00c04fb951ed}";

            var stDevice = new StDevice(deviceID);
            Assert.IsTrue(stDevice.Open(null) == StDeviceErrors.STDEVICE_NOERROR);
        }

        [TestMethod]
        public void TestGetStringDescriptor()
        {
            const string deviceID = @"\\?\usb#vid_0483&pid_df11#00000008ffff#{a5dcbf10-6530-11d2-901f-00c04fb951ed}";

            var stDevice = new StDevice(deviceID);
            Assert.IsTrue(stDevice.Open(null) == StDeviceErrors.STDEVICE_NOERROR);

            Assert.IsNotNull(stDevice.GetStringDescriptor(0));
        }

        [TestMethod]
        public void TestGetDfuDescriptor()
        {
            const string deviceID = @"\\?\usb#vid_0483&pid_df11#00000008ffff#{a5dcbf10-6530-11d2-901f-00c04fb951ed}";

            var stDevice = new StDevice(deviceID);
            Assert.IsTrue(stDevice.Open(null) == StDeviceErrors.STDEVICE_NOERROR);

            Assert.IsInstanceOfType(stDevice.DfuDescriptor, typeof(DFU_FUNCTIONAL_DESCRIPTOR));

            Assert.IsTrue(stDevice.DfuDescriptor.bcdDFUVersion >= 0x011A);
        }

        [TestMethod]
        public void TestCreateMapping()
        {
            const string deviceID = @"\\?\usb#vid_0483&pid_df11#00000008ffff#{a5dcbf10-6530-11d2-901f-00c04fb951ed}";

            var stDevice = new StDevice(deviceID);
            Assert.IsTrue(stDevice.Open(null) == StDeviceErrors.STDEVICE_NOERROR);

            var m = stDevice.CreateMapping();
        }
    }
}
