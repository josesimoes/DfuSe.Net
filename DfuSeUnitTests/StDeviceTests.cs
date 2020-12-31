using DfuSe.Core;
using DfuSe.Core.Usb;
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

            Assert.IsInstanceOfType(stDevice.DfuDescriptor, typeof(DfuFunctionalDescriptor));

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

        [TestMethod]
        public void TestGetStatus()
        {
            const ushort URB_FUNCTION_CLASS_INTERFACE = 0x001B;

            const string deviceID = @"\\?\usb#vid_0483&pid_df11#00000008ffff#{a5dcbf10-6530-11d2-901f-00c04fb951ed}";

            var stDevice = new StDevice(deviceID);
            Assert.IsTrue(stDevice.Open(null) == StDeviceErrors.STDEVICE_NOERROR);

            ControlPipeRequest request = new ControlPipeRequest
            {
                Function = URB_FUNCTION_CLASS_INTERFACE,
                Direction = (byte)RequestDirection.In,
                Request = (byte)DfuCommands.DFU_GETSTATUS,
                Value = 0,
                Index = 0,
                Length = 6
            };

            var dfuStatus = stDevice.ControlPipeRequest<DfuStatus>(request);

            Assert.IsTrue(dfuStatus.bState != 0 || dfuStatus.bStatus != 0);
        }


        [TestMethod]
        public void TestClearStatus()
        {
            const ushort URB_FUNCTION_CLASS_INTERFACE = 0x001B;

            const string deviceID = @"\\?\usb#vid_0483&pid_df11#00000008ffff#{a5dcbf10-6530-11d2-901f-00c04fb951ed}";

            var stDevice = new StDevice(deviceID);
            Assert.IsTrue(stDevice.Open(null) == StDeviceErrors.STDEVICE_NOERROR);

            ControlPipeRequest request = new ControlPipeRequest
            {
                Function = URB_FUNCTION_CLASS_INTERFACE,
                Direction = (byte)RequestDirection.Out,
                Request = (byte)DfuCommands.DFU_CLRSTATUS,
                Value = 0,
                Index = 0,
                Length = 0
            };

            stDevice.ControlPipeRequest(request);
        }
    }
}
