using System;
using System.Linq;
using Vurdalakov.UsbDevicesDotNet;

namespace usb_reliability_detector
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("USB Controllers");

            foreach (UsbDevice usbDevice in UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_HOST_CONTROLLER)))
            {
                var deviceService = usbDevice.Properties.First(p => p.HasSameKey(UsbDeviceWinApi.DevicePropertyKeys.DEVPKEY_Device_Service));
                Console.WriteLine($"ID {usbDevice.DeviceId} VID_{usbDevice.Vid}&PID_{usbDevice.Pid} {usbDevice.BusReportedDeviceDescription} {deviceService.Value}");
            }

            Console.WriteLine("\n\nUSB Hubs");

            foreach (UsbDevice usbDevice in UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_HUB)))
            {
                var deviceService = usbDevice.Properties.First(p => p.HasSameKey(UsbDeviceWinApi.DevicePropertyKeys.DEVPKEY_Device_Service));
                Console.WriteLine($"ID {usbDevice.DeviceId} VID_{usbDevice.Vid}&PID_{usbDevice.Pid} {usbDevice.BusReportedDeviceDescription} {deviceService.Value}");
                Console.WriteLine("Hub:Port = {0}:{1}", usbDevice.Hub, usbDevice.Port);
            }

            Console.WriteLine("\n\nUSB Devices");

            foreach (UsbDevice usbDevice in UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_DEVICE)))
            {
                var deviceService = usbDevice.Properties.First(p => p.HasSameKey(UsbDeviceWinApi.DevicePropertyKeys.DEVPKEY_Device_Service));
                Console.WriteLine($"ID {usbDevice.DeviceId} VID_{usbDevice.Vid}&PID_{usbDevice.Pid} {usbDevice.BusReportedDeviceDescription} {deviceService.Value}");
                Console.WriteLine("Hub:Port = {0}:{1}", usbDevice.Hub, usbDevice.Port);
            }

            Console.ReadKey();

        }
    }
}
