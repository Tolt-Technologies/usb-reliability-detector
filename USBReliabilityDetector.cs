using NLog;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using ToltTech.Telemetry;
using Vurdalakov.UsbDevicesDotNet;

namespace ToltTech
{
    internal class USBReliabilityDetector
    {
        private static Logger Logger;

        static void Main()
        {
            var consoleTarget = new ColoredConsoleTarget
            {
                EnableAnsiOutput = true
            };

            Logging.Initialize(false);
            LogManager.Configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);
            LogManager.ReconfigExistingLoggers();

            Logger = LogManager.GetCurrentClassLogger();
            Console.WriteLine($"Logging to {Logging.LogFilePath}");

            var controllers = new List<UsbDevice>(UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_HOST_CONTROLLER)));
            var hubs = new List<UsbDevice>(UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_HUB)));
            var devices = new List<UsbDevice>(UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_DEVICE)));

            Logger.Info($"Controllers {controllers.Count} Hubs {hubs.Count} Devices {devices.Count}");

            while (true)
            {
                // Duplicate the previous refresh for comparison to the new list
                var currentControllers = controllers.ToList();
                var currentHubs = hubs.ToList();
                var currentDevices = devices.ToList();

                // Refresh the USB Devices lists
                controllers = new List<UsbDevice>(UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_HOST_CONTROLLER)));
                hubs = new List<UsbDevice>(UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_HUB)));
                devices = new List<UsbDevice>(UsbDevice.GetDevices(new Guid(UsbDeviceWinApi.GUID_DEVINTERFACE_USB_DEVICE)));

                DetectChanges(currentControllers, controllers);
                DetectChanges(currentHubs, hubs);
                DetectChanges(currentDevices, devices);

                System.Threading.Thread.Sleep(100);

                ConsoleSpinner.Turn();

                if (Console.KeyAvailable) break;
            }

            LogManager.Flush(TimeSpan.FromSeconds(5));
            LogManager.Shutdown();
        }

        static void DetectChanges(List<UsbDevice> previous, List<UsbDevice> current)
        {
            foreach (var usbDevice in current)
            {
                var matchingController = previous.Find(d => d.DeviceId == usbDevice.DeviceId);
                if (matchingController == null)
                {
                    var deviceService = usbDevice.Properties.First(p => p.HasSameKey(UsbDeviceWinApi.DevicePropertyKeys.DEVPKEY_Device_Service));
                    Logger.Info($"Added: VID_{usbDevice.Vid}&PID_{usbDevice.Pid} {usbDevice.BusReportedDeviceDescription} {deviceService.Value}");
                }
                else previous.Remove(matchingController);
            }

            foreach (var usbDevice in previous)
            {
                var deviceService = usbDevice.Properties.First(p => p.HasSameKey(UsbDeviceWinApi.DevicePropertyKeys.DEVPKEY_Device_Service));
                Logger.Info($"Removed: VID_{usbDevice.Vid}&PID_{usbDevice.Pid} {usbDevice.BusReportedDeviceDescription} {deviceService.Value}");
            }
        }
    }
}
