﻿namespace Vurdalakov.UsbDevicesDotNet
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;

    internal class UsbDeviceInterfaceData : UsbDeviceBase
    {
        public static UsbDevice GetUsbDevice(IntPtr handle, UsbDeviceWinApi.SP_DEVICE_INTERFACE_DATA deviceInterfaceData)
        {
            UsbDeviceInterfaceData usbDeviceInterface = new UsbDeviceInterfaceData(handle, deviceInterfaceData);
            return usbDeviceInterface.GetDevice();
        }

        private readonly IntPtr handle;
        private UsbDeviceWinApi.SP_DEVICE_INTERFACE_DATA deviceInterfaceData;

        private UsbDeviceWinApi.SP_DEVINFO_DATA devInfoData;

        private UsbDeviceInterfaceData(IntPtr handle, UsbDeviceWinApi.SP_DEVICE_INTERFACE_DATA deviceInterfaceData)
        {
            this.handle = handle;
            this.deviceInterfaceData = deviceInterfaceData;
        }

        private UsbDevice GetDevice()
        {
            UsbDevice usbDevice = new UsbDevice
            {
                DevicePath = GetDeviceInterfaceDetail()
            };

            if (null == usbDevice.DevicePath)
            {
                return null;
            }

            usbDevice.DeviceId = GetDeviceId();
            usbDevice.Interfaces = GetInterfaces(devInfoData.DevInst, usbDevice.DeviceId);

            usbDevice.Vid = ExtractStringAfterPrefix(usbDevice.DeviceId, "VID_", 4);
            usbDevice.Pid = ExtractStringAfterPrefix(usbDevice.DeviceId, "PID_", 4);

            usbDevice.Properties = GetProperties();

            usbDevice.BusReportedDeviceDescription = usbDevice.GetPropertyValue(UsbDeviceWinApi.DevicePropertyKeys.DEVPKEY_Device_BusReportedDeviceDesc) as String;

            usbDevice.RegistryProperties = GetRegistryProperties();

            String hubAndPort = usbDevice.GetRegistryPropertyValue(UsbDeviceWinApi.DeviceRegistryPropertyKeys.SPDRP_LOCATION_INFORMATION) as String;
            usbDevice.Hub = ExtractStringAfterPrefix(hubAndPort, "Hub_#", 4);
            usbDevice.Port = ExtractStringAfterPrefix(hubAndPort, "Port_#", 4);

            return usbDevice;
        }

        private String GetDeviceInterfaceDetail()
        {

            devInfoData = new UsbDeviceWinApi.SP_DEVINFO_DATA();
            devInfoData.Size = (UInt32)Marshal.SizeOf(devInfoData);

            Boolean success = UsbDeviceWinApi.SetupDiGetDeviceInterfaceDetail(handle, ref deviceInterfaceData,
                IntPtr.Zero, 0, out uint requiredSize, ref devInfoData);

            if (success || (Marshal.GetLastWin32Error() != UsbDeviceWinApi.ERROR_INSUFFICIENT_BUFFER))
            {
                TraceError("SetupDiGetDeviceInterfaceDetail");
                return null;
            }

            IntPtr buffer = Marshal.AllocHGlobal((Int32)requiredSize);
            Marshal.WriteInt32(buffer, 8 == IntPtr.Size ? 8 : 6);

            success = UsbDeviceWinApi.SetupDiGetDeviceInterfaceDetail(handle, ref deviceInterfaceData,
                buffer, requiredSize, out _, ref devInfoData);

            String devicePath = null;

            if (success)
            {
                IntPtr stringBuffer = new IntPtr(buffer.ToInt64() + 4);
                devicePath = Marshal.PtrToStringAuto(stringBuffer);
            }
            else
            {
                TraceError("SetupDiGetDeviceInterfaceDetail");
            }

            Marshal.FreeHGlobal(buffer);

            return devicePath;
        }

        private String GetDeviceId()
        {
            String deviceId = null;

            Int32 bufferSize = 512;
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

            Int32 errorCode = UsbDeviceWinApi.CM_Get_Device_ID(devInfoData.DevInst, buffer, bufferSize, 0);

            if (UsbDeviceWinApi.ERROR_SUCCESS == errorCode)
            {
                deviceId = Marshal.PtrToStringAuto(buffer);

                int slash = deviceId.LastIndexOf('\\');
                if ((slash > 0) && (deviceId.LastIndexOf('-') > slash))
                {
                    errorCode = UsbDeviceWinApi.CM_Get_Parent(out uint devInstParent, devInfoData.DevInst, 0);

                    if (UsbDeviceWinApi.ERROR_SUCCESS == errorCode)
                    {
                        errorCode = UsbDeviceWinApi.CM_Get_Device_ID(devInstParent, buffer, bufferSize, 0);

                        if (UsbDeviceWinApi.ERROR_SUCCESS == errorCode)
                        {
                            deviceId = Marshal.PtrToStringAuto(buffer);
                        }
                        else
                        {
                            TraceError("CM_Get_Device_ID", errorCode);
                        }
                    }
                    else
                    {
                        TraceError("CM_Get_Parent", errorCode);
                    }
                }
            }
            else
            {
                TraceError("CM_Get_Device_ID", errorCode);
            }

            Marshal.FreeHGlobal(buffer);

            return deviceId;
        }

        private UsbDeviceInterface[] GetInterfaces(UInt32 devInst, String deviceId)
        {
            List<UsbDeviceInterface> ids = new List<UsbDeviceInterface>();

            Int32 errorCode = UsbDeviceWinApi.CM_Get_Child(out uint devInstChild, devInst, 0);
            if (UsbDeviceWinApi.CR_SUCCESS != errorCode)
            {
                TraceError("CM_Get_Child", errorCode);

                ids.Add(new UsbDeviceInterface(deviceId));
                return ids.ToArray();
            }

            String interfaceId = GetDeviceId(devInstChild);

            if (!String.IsNullOrEmpty(interfaceId))
            {
                ids.Add(new UsbDeviceInterface(interfaceId));

                UInt32 devInstSibling = devInstChild;
                while (true)
                {

                    errorCode = UsbDeviceWinApi.CM_Get_Sibling(out devInstSibling, devInstSibling, 0);
                    if (UsbDeviceWinApi.CR_SUCCESS != errorCode)
                    {
                        TraceError("CM_Get_Sibling", errorCode);
                        break;
                    }

                    interfaceId = GetDeviceId(devInstSibling);

                    if (!String.IsNullOrEmpty(interfaceId))
                    {
                        ids.Add(new UsbDeviceInterface(interfaceId));
                    }
                }
            }

            return ids.ToArray();
        }

        private String GetDeviceId(UInt32 devInst)
        {
            Int32 bufferSize = 1024;
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

            String deviceId = null;

            Int32 errorCode = UsbDeviceWinApi.CM_Get_Device_ID(devInst, buffer, bufferSize, 0);
            if (UsbDeviceWinApi.CR_SUCCESS == errorCode)
            {
                deviceId = Marshal.PtrToStringAuto(buffer);
            }
            else
            {
                TraceError("CM_Get_Device_ID", errorCode);
            }

            Marshal.FreeHGlobal(buffer);

            return deviceId;
        }

        private UsbDeviceProperty[] GetProperties()
        {
            Boolean success = UsbDeviceWinApi.SetupDiGetDevicePropertyKeys(handle, ref devInfoData, IntPtr.Zero, 0, out uint propertyKeyCount, 0);

            if (success || (Marshal.GetLastWin32Error() != UsbDeviceWinApi.ERROR_INSUFFICIENT_BUFFER))
            {
                TraceError("SetupDiGetDevicePropertyKeys");
                return new UsbDeviceProperty[0];
            }

            if (0 == propertyKeyCount)
            {
                return new UsbDeviceProperty[0];
            }

            List<UsbDeviceProperty> properties = new List<UsbDeviceProperty>();

            UsbDeviceWinApi.DEVPROPKEY[] propertyKeyArray = new UsbDeviceWinApi.DEVPROPKEY[propertyKeyCount];
            GCHandle propertyKeyArrayPinned = GCHandle.Alloc(propertyKeyArray , GCHandleType.Pinned);

            IntPtr buffer = propertyKeyArrayPinned.AddrOfPinnedObject();

            success = UsbDeviceWinApi.SetupDiGetDevicePropertyKeys(handle, ref devInfoData, buffer, propertyKeyCount, out propertyKeyCount, 0);

            if (success)
            {
                for (UInt32 propertyKeyIndex = 0; propertyKeyIndex < propertyKeyCount; propertyKeyIndex++)
                {
                    success = UsbDeviceWinApi.SetupDiGetDevicePropertyW(handle, ref devInfoData,
                        ref propertyKeyArray[propertyKeyIndex], out _, IntPtr.Zero, 0, out uint requiredSize, 0);

                    if (success || (Marshal.GetLastWin32Error() != UsbDeviceWinApi.ERROR_INSUFFICIENT_BUFFER))
                    {
                        TraceError("SetupDiGetDeviceProperty");
                        success = false;
                    }
                    else
                    {
                        buffer = Marshal.AllocHGlobal((Int32)requiredSize);
                        success = UsbDeviceWinApi.SetupDiGetDevicePropertyW(handle, ref devInfoData,
                                                    ref propertyKeyArray[propertyKeyIndex], out uint propertyType, buffer, requiredSize, out requiredSize, 0);

                        if (success)
                        {
                            Object value = MarshalDeviceProperty(buffer, (Int32)requiredSize, propertyType);
                            properties.Add(new UsbDeviceProperty(propertyKeyArray[propertyKeyIndex], value, propertyType));
                        }
                        else
                        {
                            TraceError("SetupDiGetDevicePropertyW");
                        }

                        Marshal.FreeHGlobal(buffer);
                    }

                    if (!success) // don't combine with previous "if", covers 2 cases
                    {
                        properties.Add(new UsbDeviceProperty(propertyKeyArray[propertyKeyIndex], null, UsbDeviceWinApi.DevicePropertyTypes.DEVPROP_TYPE_EMPTY));
                    }
                }
            }
            else
            {
                TraceError("SetupDiGetDevicePropertyKeys");
            }

            propertyKeyArrayPinned.Free();

            return properties.ToArray();
        }

        private Object MarshalDeviceProperty(IntPtr source, Int32 length, UInt32 type)
        {
            // TODO: add other types; now covers only all types that are mentioned in devpkey.h
            switch (type)
            {
                case UsbDeviceWinApi.DevicePropertyTypes.DEVPROP_TYPE_UINT32:
                    return (UInt32)Marshal.ReadInt32(source);
                case UsbDeviceWinApi.DevicePropertyTypes.DEVPROP_TYPE_GUID:
                    return MarshalEx.ReadGuid(source, length);
                case UsbDeviceWinApi.DevicePropertyTypes.DEVPROP_TYPE_FILETIME:
                    return MarshalEx.ReadFileTime(source);
                case UsbDeviceWinApi.DevicePropertyTypes.DEVPROP_TYPE_BOOLEAN:
                    return Marshal.ReadByte(source) != 0;
                case UsbDeviceWinApi.DevicePropertyTypes.DEVPROP_TYPE_STRING:
                    return Marshal.PtrToStringUni(source);
                case UsbDeviceWinApi.DevicePropertyTypes.DEVPROP_TYPE_SECURITY_DESCRIPTOR:
                    return MarshalEx.ReadSecurityDescriptor(source, length);
                case UsbDeviceWinApi.DevicePropertyTypes.DEVPROP_TYPE_SECURITY_DESCRIPTOR_STRING:
                    return Marshal.PtrToStringUni(source);
                case UsbDeviceWinApi.DevicePropertyTypes.DEVPROP_TYPE_BINARY:
                    return MarshalEx.ReadByteArray(source, length);
                case UsbDeviceWinApi.DevicePropertyTypes.DEVPROP_TYPE_STRING_LIST:
                    return MarshalEx.ReadMultiSzStringList(source, length);
                default:
                    return null;
            }
        }

        private UsbDeviceRegistryProperty[] GetRegistryProperties()
        {
            List<UsbDeviceRegistryProperty> registryProperties = new List<UsbDeviceRegistryProperty>();

            for (UInt32 property = 0; property < UsbDeviceWinApi.DeviceRegistryPropertyKeys.SPDRP_MAXIMUM_PROPERTY; property++)
            {
                Boolean success = UsbDeviceWinApi.SetupDiGetDeviceRegistryProperty(handle, ref devInfoData,
                    property, out _, IntPtr.Zero, 0, out uint requiredSize);

                if (success || (Marshal.GetLastWin32Error() != UsbDeviceWinApi.ERROR_INSUFFICIENT_BUFFER))
                {
                    if (Marshal.GetLastWin32Error() != UsbDeviceWinApi.ERROR_INVALID_DATA)
                    {
                        TraceError("SetupDiGetDeviceRegistryProperty");
                    }
                }
                else
                {
                    IntPtr buffer = Marshal.AllocHGlobal((Int32)requiredSize);
                    success = UsbDeviceWinApi.SetupDiGetDeviceRegistryProperty(handle, ref devInfoData,
                                            property, out uint propertyType, buffer, requiredSize, out requiredSize);

                    if (success)
                    {
                        Object value = MarshalDeviceRegistryProperty(buffer, (Int32)requiredSize, propertyType);
                        registryProperties.Add(new UsbDeviceRegistryProperty(property, value, propertyType));
                    }
                    else
                    {
                        TraceError("SetupDiGetDeviceRegistryProperty");
                        registryProperties.Add(new UsbDeviceRegistryProperty(property, null, UsbDeviceWinApi.DeviceRegistryPropertyTypes.REG_NONE));
                    }

                    Marshal.FreeHGlobal(buffer);
                }
            }

            return registryProperties.ToArray();
        }

        private Object MarshalDeviceRegistryProperty(IntPtr source, Int32 length, UInt32 type)
        {
            switch (type)
            {
                case UsbDeviceWinApi.DeviceRegistryPropertyTypes.REG_NONE:
                    return null;
                case UsbDeviceWinApi.DeviceRegistryPropertyTypes.REG_SZ:
                case UsbDeviceWinApi.DeviceRegistryPropertyTypes.REG_EXPAND_SZ:
                    return Marshal.PtrToStringAuto(source);
                case UsbDeviceWinApi.DeviceRegistryPropertyTypes.REG_BINARY:
                    return MarshalEx.ReadByteArray(source, length);
                case UsbDeviceWinApi.DeviceRegistryPropertyTypes.REG_DWORD:
              //case UsbDeviceWinApi.DeviceRegistryPropertyTypes.REG_DWORD_LITTLE_ENDIAN:
                    return (UInt32)Marshal.ReadInt32(source);
                case UsbDeviceWinApi.DeviceRegistryPropertyTypes.REG_DWORD_BIG_ENDIAN:
                    return Endianness.Swap((UInt32)Marshal.ReadInt32(source));
                case UsbDeviceWinApi.DeviceRegistryPropertyTypes.REG_LINK:
                    return Marshal.PtrToStringUni(source);
                case UsbDeviceWinApi.DeviceRegistryPropertyTypes.REG_MULTI_SZ:
                    return MarshalEx.ReadMultiSzStringList(source, length);
                case UsbDeviceWinApi.DeviceRegistryPropertyTypes.REG_RESOURCE_LIST:
                    return null; // TODO
                case UsbDeviceWinApi.DeviceRegistryPropertyTypes.REG_FULL_RESOURCE_DESCRIPTOR:
                    return null; // TODO
                case UsbDeviceWinApi.DeviceRegistryPropertyTypes.REG_RESOURCE_REQUIREMENTS_LIST:
                    return null; // TODO
                case UsbDeviceWinApi.DeviceRegistryPropertyTypes.REG_QWORD:
              //case UsbDeviceWinApi.DeviceRegistryPropertyTypes.REG_QWORD_LITTLE_ENDIAN:
                    return (UInt64)Marshal.ReadInt64(source);
                default:
                    return null;
            }
        }

        private String ExtractStringAfterPrefix(String text, String prefix, Int32 length)
        {
            if (String.IsNullOrEmpty(text))
            {
                return null;
            }

            Int32 index = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            return index >= 0 ? text.Substring(index + prefix.Length, length).ToUpper() : null;
        }
    }
}
