using NLog;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using ToltTech.Telemetry;
using Vurdalakov.UsbDevicesDotNet;

namespace usb_reliability_detector
{
    internal class Program
    {
        private static Logger Logger;

        static void Main(string[] args)
        {
            var consoleTarget = new ColoredConsoleTarget
            {
                EnableAnsiOutput = true
            };

            Logging.Initialize(false);
            LogManager.Configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);
            LogManager.ReconfigExistingLoggers();

            Logger = LogManager.GetCurrentClassLogger();
            Logger.Info($"{AppInfo.Name} {AppInfo.Version} starting");

            var controllers = new List<UsbDevice>(UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_HOST_CONTROLLER)));
            var hubs = new List<UsbDevice>(UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_HUB)));
            var devices = new List<UsbDevice>(UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_DEVICE)));

            Logger.Info($"Controllers {controllers.Count} Hubs {hubs.Count} Devices {devices.Count}");

            while (true)
            {
                var currentControllers = controllers.ToList();
                var currentHubs = hubs.ToList();
                var currentDevices = devices.ToList();

                // Refresh the USB Devices lists
                controllers = new List<UsbDevice>(UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_HOST_CONTROLLER)));
                hubs = new List<UsbDevice>(UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_HUB)));
                devices = new List<UsbDevice>(UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_DEVICE)));

                foreach(var usbDevice in controllers) {
                    var matchingController = currentControllers.Find(d => d.DeviceId == usbDevice.DeviceId);
                    if (matchingController == null)
                    {
                        var deviceService = usbDevice.Properties.First(p => p.HasSameKey(UsbDeviceWinApi.DevicePropertyKeys.DEVPKEY_Device_Service));
                        Logger.Warn($"Controller added: ID {usbDevice.DeviceId} VID_{usbDevice.Vid}&PID_{usbDevice.Pid} {usbDevice.BusReportedDeviceDescription} {deviceService.Value}");
                    }
                    else currentControllers.Remove(matchingController);
                }

                foreach(var usbDevice in currentControllers)
                {
                    var deviceService = usbDevice.Properties.First(p => p.HasSameKey(UsbDeviceWinApi.DevicePropertyKeys.DEVPKEY_Device_Service));
                    Logger.Warn($"Controller removed: ID {usbDevice.DeviceId} VID_{usbDevice.Vid}&PID_{usbDevice.Pid} {usbDevice.BusReportedDeviceDescription} {deviceService.Value}");
                }

                foreach (var usbDevice in hubs)
                {
                    var matching = currentHubs.Find(d => d.DeviceId == usbDevice.DeviceId);
                    if (matching == null)
                    {
                        var deviceService = usbDevice.Properties.First(p => p.HasSameKey(UsbDeviceWinApi.DevicePropertyKeys.DEVPKEY_Device_Service));
                        Logger.Warn($"Hub added: ID {usbDevice.DeviceId} VID_{usbDevice.Vid}&PID_{usbDevice.Pid} {usbDevice.BusReportedDeviceDescription} {deviceService.Value}");
                    }
                    else currentHubs.Remove(matching);
                }

                foreach (var usbDevice in currentHubs)
                {
                    var deviceService = usbDevice.Properties.First(p => p.HasSameKey(UsbDeviceWinApi.DevicePropertyKeys.DEVPKEY_Device_Service));
                    Logger.Warn($"Hub removed: ID {usbDevice.DeviceId} VID_{usbDevice.Vid}&PID_{usbDevice.Pid} {usbDevice.BusReportedDeviceDescription} {deviceService.Value}");
                }

                foreach (var usbDevice in devices)
                {
                    var matching = currentDevices.Find(d => d.DeviceId == usbDevice.DeviceId);
                    if (matching == null)
                    {
                        var deviceService = usbDevice.Properties.First(p => p.HasSameKey(UsbDeviceWinApi.DevicePropertyKeys.DEVPKEY_Device_Service));
                        Logger.Warn($"Device added: ID {usbDevice.DeviceId} VID_{usbDevice.Vid}&PID_{usbDevice.Pid} {usbDevice.BusReportedDeviceDescription} {deviceService.Value}");
                    }
                    else currentDevices.Remove(matching);
                }

                foreach (var usbDevice in currentDevices)
                {
                    var deviceService = usbDevice.Properties.First(p => p.HasSameKey(UsbDeviceWinApi.DevicePropertyKeys.DEVPKEY_Device_Service));
                    Logger.Warn($"Device removed: ID {usbDevice.DeviceId} VID_{usbDevice.Vid}&PID_{usbDevice.Pid} {usbDevice.BusReportedDeviceDescription} {deviceService.Value}");
                }

                if (Console.KeyAvailable) break;
            }

            Logger.Info($"{AppInfo.Name} {AppInfo.Version} closing due to keypress");

            NLog.LogManager.Flush(TimeSpan.FromSeconds(5));
            NLog.LogManager.Shutdown();
        }
    }
}
