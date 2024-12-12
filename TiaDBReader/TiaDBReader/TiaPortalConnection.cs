using System;
using Siemens.Engineering;
using System.Linq;
using System.Collections.Generic;

namespace TiaDBReader
{
    public class TiaPortalConnection
    {
        private TiaPortal _tiaPortal;

        public TiaPortal Connect()
        {
            try
            {
                Console.WriteLine("Attempting to connect to TIA Portal...");
                IList<TiaPortalProcess> processes = TiaPortal.GetProcesses();

                if (processes.Count > 0)
                {
                    _tiaPortal = processes[0].Attach();
                    Console.WriteLine("Successfully connected to existing TIA Portal instance");
                    return _tiaPortal;
                }

                Console.WriteLine("No running TIA Portal instances found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to TIA Portal: {ex.Message}");
                return null;
            }
        }

        public Project GetCurrentProject()
        {
            if (_tiaPortal?.Projects.Count > 0)
            {
                var project = _tiaPortal.Projects[0];
                Console.WriteLine($"\nWorking with project: {project.Name}");
                return project;
            }

            Console.WriteLine("\nNo projects currently open");
            return null;
        }

        public void Disconnect()
        {
            _tiaPortal?.Dispose();
            _tiaPortal = null;
        }
    }
}