using System;
using System.Collections.Generic;
using System.Linq;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;

namespace TiaDBReader
{
    public class PlcBrowser
    {
        public IEnumerable<PlcSoftware> GetAllPlcSoftwares(Project project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            foreach (var device in project.Devices)
            {
                var ret = GetPlcSoftware(device);
                if (ret != null)
                    yield return ret;
            }
        }

        private PlcSoftware GetPlcSoftware(Device device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            foreach (var devItem in device.DeviceItems)
            {
                var target = ((IEngineeringServiceProvider)devItem).GetService<SoftwareContainer>();
                if (target != null && target.Software is PlcSoftware)
                    return (PlcSoftware)target.Software;
            }

            return null;
        }

        public PlcSoftware SelectPLC(List<PlcSoftware> plcSoftwares)
        {
            try
            {
                if (!plcSoftwares.Any())
                {
                    Console.WriteLine("No PLCs found in project");
                    return null;
                }

                Console.WriteLine("\nAvailable PLCs:");
                for (int i = 0; i < plcSoftwares.Count; i++)
                {
                    var plc = plcSoftwares[i];
                    var softwareContainer = plc.Parent as SoftwareContainer;
                    var deviceItem = softwareContainer?.Parent as DeviceItem;
                    string deviceName = deviceItem?.Name ?? "Unknown Device";
                    Console.WriteLine($"{i + 1}. PLC: {deviceName}");
                }

                Console.Write("\nSelect PLC number: ");
                if (int.TryParse(Console.ReadLine(), out int selection) &&
                    selection > 0 &&
                    selection <= plcSoftwares.Count)
                {
                    var selectedPlc = plcSoftwares[selection - 1];
                    var softwareContainer = selectedPlc.Parent as SoftwareContainer;
                    var deviceItem = softwareContainer?.Parent as DeviceItem;
                    string deviceName = deviceItem?.Name ?? "Unknown Device";

                    Console.WriteLine($"Selected PLC: {deviceName}");
                    return selectedPlc;
                }

                Console.WriteLine("Invalid selection");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error selecting PLC: {ex.Message}");
                return null;
            }
        }
    }
}