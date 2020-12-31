using DfuSe.Core.Windows;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DfuSe.Core
{
	public class StDevice
	{
		int m_CurrentConfig;
		int m_CurrentInterf;
		int m_CurrentAltSet;

		SafeFileHandle m_DeviceHandle;
		bool m_bDeviceIsOpen;
		uint m_nDefaultTimeOut;

		USB_DEVICE_DESCRIPTOR m_DeviceDescriptor;
		List<byte[]> m_pConfigs;

		//HANDLE* m_pPipeHandles;
		uint m_nbEndPoints;

		public string SymbolicName { get; }

        public USB_DEVICE_DESCRIPTOR DeviceDescriptor => m_DeviceDescriptor;

        public byte NumberOfConfigurations => DeviceDescriptor.bNumConfigurations;

        public List<USB_CONFIGURATION_DESCRIPTOR> ConfigurationDescriptors;
        private byte NumAlternates;

        public DfuFunctionalDescriptor DfuDescriptor { get; private set; }

        public USB_INTERFACE_DESCRIPTOR DfuInterface { get; private set; }

		internal int DfuInterfaceIndex => DfuInterface.bInterfaceNumber;

        internal SafeFileHandle DeviceHandle => m_DeviceHandle;

		public StDevice(string symbolicName)
		{
			SymbolicName = symbolicName;
		}

		public StDeviceErrors Open(SafeFileHandle unplugEvent)
		{
			if (m_bDeviceIsOpen)
			{
				return StDeviceErrors.STDEVICE_NOERROR;
			}

			// 1) open the device
			var nRet = OpenDevice(unplugEvent);

			if (nRet == StDeviceErrors.STDEVICE_NOERROR)
			{
				// 2) Get the USB descriptors
				nRet = GetUsbDescriptors();

				// 3) Get DFU descriptor
				DfuDescriptor = GetDfuDescriptor();

				if (nRet != StDeviceErrors.STDEVICE_NOERROR)
				{
					CloseDevice();
				}
				else
				{
					m_bDeviceIsOpen = true;
				}
			}

			return nRet;
		}

		public void GetStatus()
        {

        }

        public T ControlPipeRequest<T>(ControlPipeRequest request)
        {
			if (!m_bDeviceIsOpen)
			{
				// STDEVICE_DRIVERISCLOSED;
				return default(T);
			}

			// setup buffer fr device descriptor
			return Win32.DeviceIoControl<T, ControlPipeRequest>(
				m_DeviceHandle,
				EIOControlCode.VendorRequest,
				request);
		}

		public void ControlPipeRequest(ControlPipeRequest request)
		{
			if (!m_bDeviceIsOpen)
			{
				// STDEVICE_DRIVERISCLOSED;
				return;
			}

			// setup buffer fr device descriptor
			Win32.DeviceIoControl(
				m_DeviceHandle,
				EIOControlCode.VendorRequest,
				request);
		}

		public StDeviceErrors Close()
		{
			if (!m_bDeviceIsOpen)
			{
				return StDeviceErrors.STDEVICE_NOERROR;
			}

			// 1. Close the pipes, if needed
			ClosePipes();

			// 2. Release the descriptors
			ReleaseDescriptors();

			// 3. Close the device
			CloseDevice();

			m_bDeviceIsOpen = false;

			return StDeviceErrors.STDEVICE_NOERROR;
		}

		public string GetStringDescriptor(byte index)
		{
			if (!m_bDeviceIsOpen)
			{
				return "";
			}

			// request is comprised with index and language (US code)
			var request = new byte[] { index, 0x04, 0x09 };

			// setup buffer fir device descriptor
			var descriptorBuffer = Win32.DeviceIoControl(
				m_DeviceHandle,
				EIOControlCode.GetStringDescriptor,
				request);

			// sanity check
			if (descriptorBuffer.Length > 2 &&
				descriptorBuffer[1] == 3)
			{
				// descriptor is encoded 2 bytes per char
				return new string(Encoding.Unicode.GetChars(descriptorBuffer, 2, descriptorBuffer.Length - 2));
			}

			return null;
		}

		public List<Mapping> CreateMapping()
        {
			if (!m_bDeviceIsOpen)
			{
				// TODO
				//return StDeviceErrors.STDEVICE_NOERROR;
				return null;
			}

			var map = new List<Mapping>(NumAlternates + 1);

			for(int i = 0; i < NumAlternates; i++)
            {
				Mapping mapping = new Mapping();
				mapping.Sectors = new List<Mapping.Sector>();

				USB_INTERFACE_DESCRIPTOR ItfDesc = GetInterfaceDescriptor(DfuInterfaceIndex, i);

				// sanity check
				if(ItfDesc.iInterface == 0)
                {
                    // TODO
                    //STDFUPRT_BADPARAMETER
                    break;
				}

				var mapDescription = GetStringDescriptor(ItfDesc.iInterface);

				if(!mapDescription.StartsWith("@"))
                {
					// TODO
					//STDFUPRT_BADPARAMETER
					break;
				}

				// parse the string
				var mapDescriptionDetails = mapDescription.Split('/');

				// store mapping name, dropping '@'
				mapping.Name = mapDescriptionDetails[0].Substring(1);

				mapping.Alternate = ItfDesc.bAlternateSetting;

				// start address
				var startAddress = Convert.ToInt32(mapDescriptionDetails[1], 16);
				var currentAddress = startAddress;

				string sectorInfoPattern = @"(\d{2}).(\d{3})(K|M| )(.)";
				Regex rg = new Regex(sectorInfoPattern);

				MatchCollection sectors = rg.Matches(mapDescription);

				int numberOfSectors = 0, currentSector = 0;
				foreach(Match s in sectors)
                {
					Mapping.Sector sector = new Mapping.Sector();

					// number of pages
					var pages = int.Parse(s.Groups[1].Value, System.Globalization.NumberStyles.Integer);

					// page size
					var pageSize = int.Parse(s.Groups[2].Value, System.Globalization.NumberStyles.Integer);

					// size multiplier
					var multiplier = 1;
					if(s.Groups[3].Value == "K")
                    {
						// k = 1024
						multiplier = 1024;
					}
					else if (s.Groups[3].Value == "M")
					{
						// M = 1024*1024
						multiplier = 1024 * 1024;
					}

					sector.StartAddress = currentAddress;

					sector.SectorIndex = currentSector++;
					sector.SectorSize = pages * pageSize * multiplier;
					sector.SectorType = (byte)s.Groups[4].Value[0];
					sector.UseForOperation = true;

					mapping.Sectors.Add(sector);

					// update current address
					currentAddress += sector.SectorSize;

					// update sector count
					mapping.NumberOfSectors++;
				}

				map.Add(mapping);
			}

			return map;
		}

		private byte GetNumberOfAlternates(int interfaceIndex)
		{
			USB_INTERFACE_DESCRIPTOR lastInterface = null;
			byte interfaceCount = 0;
			byte alternatesCount = 1;

			var totalLength = ConfigurationDescriptors[0].wTotalLength;

			var index = m_pConfigs[interfaceIndex][0];

			while (index < totalLength)
			{
				if (m_pConfigs[interfaceIndex][index + 1] == 0x02 /*USB_CONFIGURATION_DESCRIPTOR_TYPE*/)
				{
					// TODO
					//return STDEVICE_DESCRIPTORNOTFOUND;
				}

				if (m_pConfigs[interfaceIndex][index + 1] == 0x04 /*USB_INTERFACE_DESCRIPTOR_TYPE*/)
				{
					// this is a valid USB_INTERFACE_DESCRIPTOR
					// load respective USB Interface descriptor struct
					// need to use unamged pointer
					IntPtr pointerToDescriptor = Marshal.AllocHGlobal(USB_INTERFACE_DESCRIPTOR.DefaultSize);
					Marshal.Copy(m_pConfigs[interfaceIndex], index, pointerToDescriptor, USB_INTERFACE_DESCRIPTOR.DefaultSize);

					var usbInterfaceDescriptor = (USB_INTERFACE_DESCRIPTOR)Marshal.PtrToStructure(pointerToDescriptor, typeof(USB_INTERFACE_DESCRIPTOR));

					Marshal.FreeHGlobal(pointerToDescriptor);

					if (lastInterface != null)
					{
						if (interfaceCount == interfaceIndex)
						{
							// check if this interface descriptor is a different interface or just an alternate of the same
							if (lastInterface.bInterfaceNumber == usbInterfaceDescriptor.bInterfaceNumber)
							{
								alternatesCount++;
							}
							else
							{
								if (alternatesCount > 1)
								{
									break;
								}
							}
						}
						else
						{
							interfaceCount++;
						}
					}

					// store the last interface
					lastInterface = usbInterfaceDescriptor;

				}

				// move index to the next descriptor
				index += m_pConfigs[interfaceIndex][index];
			}

			return alternatesCount;
		}

		private DfuFunctionalDescriptor GetDfuDescriptor()
		{
			var numInterfaces = ConfigurationDescriptors[0].bNumInterfaces;

			NumAlternates = GetNumberOfAlternates(numInterfaces - 1);

			var dfuDescriptor = GetDescriptor<DfuFunctionalDescriptor>(numInterfaces - 1, NumAlternates - 1, 0, DfuFunctionalDescriptor.Type, DfuFunctionalDescriptor.DefaultSize);

			DfuInterface = GetInterfaceDescriptor(numInterfaces - 1, 0);

			return dfuDescriptor;
		}

		private USB_INTERFACE_DESCRIPTOR GetInterfaceDescriptor(int interfaceIndex, int alternateIndex)
		{
			USB_INTERFACE_DESCRIPTOR lastInterface = null;
			USB_INTERFACE_DESCRIPTOR usbInterfaceDescriptor;
			IntPtr pointerToDescriptor;
			byte interfaceCount = 0;
			byte alternatesCount = 1;

			var totalLength = ConfigurationDescriptors[0].wTotalLength;

			var index = m_pConfigs[interfaceIndex][0];

			while (index < totalLength)
			{
				if (m_pConfigs[interfaceIndex][index + 1] == 0x02 /*USB_CONFIGURATION_DESCRIPTOR_TYPE*/)
				{
					// TODO
					//return STDEVICE_DESCRIPTORNOTFOUND;
				}

				if (m_pConfigs[interfaceIndex][index + 1] == 0x04 /*USB_INTERFACE_DESCRIPTOR_TYPE*/)
				{
					// this is a valid USB_INTERFACE_DESCRIPTOR
					// load respective USB Interface descriptor struct
					// need to use unamged pointer
					pointerToDescriptor = Marshal.AllocHGlobal(USB_INTERFACE_DESCRIPTOR.DefaultSize);
					Marshal.Copy(m_pConfigs[interfaceIndex], index, pointerToDescriptor, USB_INTERFACE_DESCRIPTOR.DefaultSize);

					usbInterfaceDescriptor = (USB_INTERFACE_DESCRIPTOR)Marshal.PtrToStructure(pointerToDescriptor, typeof(USB_INTERFACE_DESCRIPTOR));

					Marshal.FreeHGlobal(pointerToDescriptor);

					if (lastInterface != null)
					{
						if (interfaceCount == interfaceIndex)
						{
							// check if this interface descriptor is a different interface or just an alternate of the same
							if (lastInterface.bInterfaceNumber == usbInterfaceDescriptor.bInterfaceNumber)
							{
								if (alternatesCount == alternateIndex)
								{
									return usbInterfaceDescriptor;
								}
								else
								{
									alternatesCount++;
								}
							}
							else
							{
								if (alternatesCount > 0)
								{
									// TODO
									// return STDEVICE_DESCRIPTORNOTFOUND;
								}
							}
						}
						else
						{
							interfaceCount++;
						}
					}
					else
					{
						if (interfaceIndex == 0 &&
							alternateIndex == 0)
						{
							// this is a valid USB_INTERFACE_DESCRIPTOR
							// load respective USB Interface descriptor struct
							// need to use unamged pointer
							pointerToDescriptor = Marshal.AllocHGlobal(USB_INTERFACE_DESCRIPTOR.DefaultSize);
							Marshal.Copy(m_pConfigs[interfaceIndex], index, pointerToDescriptor, USB_INTERFACE_DESCRIPTOR.DefaultSize);

							usbInterfaceDescriptor = (USB_INTERFACE_DESCRIPTOR)Marshal.PtrToStructure(pointerToDescriptor, typeof(USB_INTERFACE_DESCRIPTOR));

							Marshal.FreeHGlobal(pointerToDescriptor);

							return usbInterfaceDescriptor;
						}
					}

					// store the last interface
					lastInterface = usbInterfaceDescriptor;
				}

				// move index to the next descriptor
				index += m_pConfigs[interfaceIndex][index];
			}

			return null;
		}

		private T GetDescriptor<T>(int interfaceIndex, int alternateIndex, int targetIndex, int type, int descriptorSize)
		{
			USB_INTERFACE_DESCRIPTOR lastInterface = null;
			T descriptor = default(T);
			byte interfaceCount = 0;
			byte alternatesCount = 1;
			int counter = -1;

			var totalLength = ConfigurationDescriptors[0].wTotalLength;

			var index = m_pConfigs[interfaceIndex][0];

			while (index < totalLength)
			{
				if (m_pConfigs[interfaceIndex][index + 1] == 0x02 /*USB_CONFIGURATION_DESCRIPTOR_TYPE*/)
				{
					// TODO
					//return STDEVICE_DESCRIPTORNOTFOUND;
				}

				if (m_pConfigs[interfaceIndex][index + 1] == 0x04 /*USB_INTERFACE_DESCRIPTOR_TYPE*/)
				{
					// this is a valid USB_INTERFACE_DESCRIPTOR
					// load respective USB Interface descriptor struct
					// need to use unamged pointer
					IntPtr pointerToInterfaceDescriptor = Marshal.AllocHGlobal(USB_INTERFACE_DESCRIPTOR.DefaultSize);
					Marshal.Copy(m_pConfigs[interfaceIndex], index, pointerToInterfaceDescriptor, USB_INTERFACE_DESCRIPTOR.DefaultSize);

					var usbInterfaceDescriptor = (USB_INTERFACE_DESCRIPTOR)Marshal.PtrToStructure(pointerToInterfaceDescriptor, typeof(USB_INTERFACE_DESCRIPTOR));

					Marshal.FreeHGlobal(pointerToInterfaceDescriptor);

					if (lastInterface != null)
					{
						if (interfaceCount == interfaceIndex)
						{
							if (alternatesCount == alternateIndex)
							{
								// found the interface and alternate set

								// update index
								index += m_pConfigs[interfaceIndex][index];

								if ((m_pConfigs[interfaceIndex][index + 1] == 0x02 /*USB_CONFIGURATION_DESCRIPTOR_TYPE*/) ||
									(m_pConfigs[interfaceIndex][index + 1] == 0x04 /*USB_INTERFACE_DESCRIPTOR_TYPE*/) ||
									(m_pConfigs[interfaceIndex][index + 1] == 0x05 /*USB_ENDPOINT_DESCRIPTOR_TYPE*/))
								{
									// TODO
									//return STDEVICE_DESCRIPTORNOTFOUND;
								}

								if (m_pConfigs[interfaceIndex][index + 1] == type)
								{
									counter++;
									if (counter == targetIndex)
									{
										if (descriptorSize < m_pConfigs[interfaceIndex][index])
										{
											//nRet = STDEVICE_INCORRECTBUFFERSIZE;
										}
										else
										{
											// this is a valid descriptor
											// load respective struct
											// need to use unamged pointer
											IntPtr pointerToDescriptor = Marshal.AllocHGlobal(descriptorSize);
											Marshal.Copy(m_pConfigs[interfaceIndex], index, pointerToDescriptor, descriptorSize);

											descriptor = (T)Marshal.PtrToStructure(pointerToDescriptor, typeof(T));

											Marshal.FreeHGlobal(pointerToDescriptor);

											//nRet = STDEVICE_NOERROR;
											//memcpy(pDesc, pTmp, pTmp[0]);
											break;
										}
									}
								}
							}
							else
							{
								alternatesCount++;
							}
							//// check if this interface descriptor is a different interface or just an alternate of the same
							//if (lastInterface.bInterfaceNumber == usbInterfaceDescriptor.bInterfaceNumber)
							//{
							//	alternatesCount++;
							//}
							//else
							//{
							//	if (alternatesCount > 1)
							//	{
							//		break;
							//	}
							//}
						}
						else
						{
							interfaceCount++;
						}
					}
					else
					{
						// Do we need to access interface 0 and altset 0 ?
						if ((interfaceIndex == 0) && (alternateIndex == 0))
						{
							// update index
							index += m_pConfigs[interfaceIndex][index];

							// Yes! We are in the good place. Let's search the wanted descriptor
							while (index < totalLength)
							{
								if ((m_pConfigs[interfaceIndex][index + 1] == 0x02 /*USB_CONFIGURATION_DESCRIPTOR_TYPE*/) ||
									(m_pConfigs[interfaceIndex][index + 1] == 0x04 /*USB_INTERFACE_DESCRIPTOR_TYPE*/) ||
									(m_pConfigs[interfaceIndex][index + 1] == 0x05 /*USB_ENDPOINT_DESCRIPTOR_TYPE*/))
								{
									// TODO
									//return STDEVICE_DESCRIPTORNOTFOUND;
								}

								if (m_pConfigs[interfaceIndex][index + 1] == type)
								{
									counter++;
									if (counter == targetIndex)
									{
										if (descriptorSize < m_pConfigs[interfaceIndex][index])
										{
											//nRet = STDEVICE_INCORRECTBUFFERSIZE;
										}
										else
										{
											// this is a valid descriptor
											// load respective struct
											// need to use unamged pointer
											IntPtr pointerToDescriptor = Marshal.AllocHGlobal(descriptorSize);
											Marshal.Copy(m_pConfigs[interfaceIndex], index, pointerToDescriptor, descriptorSize);

											descriptor = (T)Marshal.PtrToStructure(pointerToDescriptor, typeof(T));

											Marshal.FreeHGlobal(pointerToDescriptor);

											//nRet = STDEVICE_NOERROR;
											//memcpy(pDesc, pTmp, pTmp[0]);
											break;
										}
									}
								}
								index += m_pConfigs[interfaceIndex][index];
							}
							break;
						}

					}

					// store the last interface
					lastInterface = usbInterfaceDescriptor;

				}

				// move index to the next descriptor
				index += m_pConfigs[interfaceIndex][index];
			}

			return descriptor;
		}

		private StDeviceErrors GetUsbDescriptors()
		{
			ReleaseDescriptors();

			try
			{
				m_DeviceDescriptor = Win32.DeviceIoControl<USB_DEVICE_DESCRIPTOR>(m_DeviceHandle, EIOControlCode.GetDeviceDescriptor);

				// setup the configs and...
				m_pConfigs = new List<byte[]>(m_DeviceDescriptor.bNumConfigurations);
				// ... the configuration descriptors
				ConfigurationDescriptors = new List<USB_CONFIGURATION_DESCRIPTOR>(m_DeviceDescriptor.bNumConfigurations);

				// Get the full configuration
				for (int i = 0; i < m_DeviceDescriptor.bNumConfigurations; i++)
				{
					// get configuration descriptor
					m_pConfigs.Insert(i, Win32.DeviceIoControl(
						m_DeviceHandle,
						EIOControlCode.GetConfigDescriptor,
						(uint)i));

					// load respective USB configuration descriptor struct
					// need to use unamged pointer
					IntPtr pointerToConfig = Marshal.AllocHGlobal(m_pConfigs[i].Length);
					Marshal.Copy(m_pConfigs[i], 0, pointerToConfig, m_pConfigs[i].Length);

					var usbConfigurationDescriptor = (USB_CONFIGURATION_DESCRIPTOR)Marshal.PtrToStructure(pointerToConfig, typeof(USB_CONFIGURATION_DESCRIPTOR));

					ConfigurationDescriptors.Insert(i, usbConfigurationDescriptor);

					Marshal.FreeHGlobal(pointerToConfig);
				}
			}
			catch (Exception ex)
			{
				// TODO output exception

				return StDeviceErrors.STDEVICE_ERRORDESCRIPTORBUILDING;
			}

			return StDeviceErrors.STDEVICE_NOERROR;
		}

		private StDeviceErrors OpenDevice(SafeFileHandle unPlugEvent)
		{
			// Close first
			CloseDevice();

			m_DeviceHandle = Win32.CreateFileW(
				SymbolicName,
				EFileAccess.GenericWrite | EFileAccess.GenericRead,
				EFileShare.None,
				IntPtr.Zero,
				ECreationDisposition.OpenExisting,
				EFileAttributes.None,
				IntPtr.Zero);

			if (!m_DeviceHandle.IsInvalid)
			{
				var bFake = m_bDeviceIsOpen;
				StDeviceErrors nRet = StDeviceErrors.STDEVICE_NOERROR;

				m_bDeviceIsOpen = true;

				// BUG BUG: Do not issue a reset as Composite devices do not support this !
				//nRet=Reset();

				m_bDeviceIsOpen = bFake;

				// The symbolic name exists. Let's create the disconnect event if needed
				if ((nRet == StDeviceErrors.STDEVICE_NOERROR) && unPlugEvent != null)
				{
					//*phUnPlugEvent = CreateEvent(NULL, FALSE, FALSE, NULL); // Disconnect event;

					//if (*phUnPlugEvent)
					//{
					//	DWORD ByteCount;
					//	if (DeviceIoControl(m_DeviceHandle,
					//						PU_SET_EVENT_DISCONNECT,
					//						phUnPlugEvent,
					//						sizeof(HANDLE),
					//						NULL,
					//						0,
					//						&ByteCount,
					//						NULL))
					//		nRet = STDEVICE_NOERROR;
					//	else
					//		nRet = STDEVICE_CANTUSEUNPLUGEVENT;
					//}
				}
				return nRet;
			}

			return StDeviceErrors.STDEVICE_OPENDRIVERERROR;
		}

		private StDeviceErrors CloseDevice()
		{
			if (m_DeviceHandle != null)
			{
				m_DeviceHandle.Close();
				m_DeviceHandle = null;
			}

			return StDeviceErrors.STDEVICE_NOERROR;
		}

		private void ReleaseDescriptors()
		{
			if (m_pConfigs != null)
			{
				m_pConfigs = null;
			}
		}

		private void ClosePipes()
		{
			//if(m_pPipeHandles)
            {
				for(int i = 0; i < m_nbEndPoints; i++)
                {
					//if(m_pPipeHandles[i] != null)
     //               {
					//	m_pPipeHandles.Close();
					//	m_pPipeHandles = null;
					//}
                }

				m_nbEndPoints = 0;
				//m_pPipeHandles = null;
			}
		}
	}
}
