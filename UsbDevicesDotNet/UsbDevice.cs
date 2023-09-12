namespace Vurdalakov.UsbDevicesDotNet
{
    using System;
    using System.Collections.Generic;

    public class UsbDevice
    {
        public String Vid { get; internal set; }
        public String Pid { get; internal set; }

        public String Hub { get; internal set; }
        public String Port { get; internal set; }

        public String DevicePath { get; internal set; }
        public String DeviceId { get; internal set; }

        public UsbDeviceInterface[] Interfaces { get; internal set; }

        public String BusReportedDeviceDescription { get; internal set; }

        public UsbDeviceProperty[] Properties { get; internal set; }

        public UsbDeviceRegistryProperty[] RegistryProperties { get; internal set; }

        internal UsbDevice()
        {
        }

        public UsbDeviceProperty GetProperty(UsbDeviceWinApi.DEVPROPKEY devPropKey)
        {
            for (Int32 i = 0; i < Properties.Length; i++)
            {
                if (Properties[i].HasSameKey(devPropKey))
                {
                    return Properties[i];
                }
            }

            return null;
        }

        public Object GetPropertyValue(UsbDeviceWinApi.DEVPROPKEY devPropKey)
        {
            UsbDeviceProperty usbDeviceProperty = GetProperty(devPropKey);
            return usbDeviceProperty?.Value;
        }

        public UsbDeviceRegistryProperty GetRegistryProperty(UInt32 key)
        {
            for (Int32 i = 0; i < RegistryProperties.Length; i++)
            {
                if (RegistryProperties[i].HasSameKey(key))
                {
                    return RegistryProperties[i];
                }
            }

            return null;
        }

        public Object GetRegistryPropertyValue(UInt32 key)
        {
            UsbDeviceRegistryProperty usbDeviceRegistryProperty = GetRegistryProperty(key);
            return usbDeviceRegistryProperty?.Value;
        }

        public static UsbDevice[] GetDevices()
        {
            return UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_DEVICE));
        }

        public static UsbDevice[] GetDevices(Guid classGuid)
        {
            using (UsbDevices usbDevices = new UsbDevices(classGuid))
            {
                return usbDevices.GetDevices();
            }
        }
    }
}
