using System;
using System.Linq;
using TiaDBReader.Models;
using System.IO;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using System.Threading;
using TiaDBReader.Services;
using Siemens.Engineering.SW;
using Siemens.Engineering;



namespace TiaDBReader
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Initialize helper classes
                var projectScanner = new ProjectScanner();
                var dbBrowser = new DBBrowser();
                var plcBrowser = new PlcBrowser();
                var xmlExtractor = new XMLCommentExtractor();
                var excelExporter = new ExcelExporter();

                // Step 1: Discover Projects
                Console.WriteLine("\n=== Discovering TIA Projects ===");
                var projects = projectScanner.DiscoverProjects();

                if (!projects.Any())
                {
                    Console.WriteLine("No valid projects found. Exiting.");
                    return;
                }

                // Step 2: Read DBs from File
                Console.WriteLine("\n=== Loading DB List from DBs.txt ===");
                string dbListPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DBs", "DBs.txt");
                var dbKeys = dbBrowser.ReadDBNamesWithKeysFromFile(dbListPath);

                if (!dbKeys.Any())
                {
                    Console.WriteLine("No DB names or CustomKeys loaded from DBs.txt. Exiting.");
                    return;
                }

                // Step 3: Launch TIA Portal in Headless Mode
                Console.WriteLine("\n=== Launching TIA Portal in headless mode ===");
                using (var tiaPortal = new TiaPortal(TiaPortalMode.WithoutUserInterface))
                {
                    Console.WriteLine("TIA Portal launched successfully.");

                    // Step 4: Export DBs from All Projects
                    foreach (var projectInfo in projects)
                    {
                        if (!projectInfo.IsValid)
                        {
                            Console.WriteLine($"Skipping invalid project: {projectInfo.ProjectName}");
                            continue;
                        }

                        Console.WriteLine($"\nProcessing project: {projectInfo.ProjectName}");

                        try
                        {
                            var project = tiaPortal.Projects.Open(new FileInfo(projectInfo.ProjectPath));
                            try
                            {
                                Console.WriteLine($"Opened project: {projectInfo.ProjectName}");

                                // Retrieve all PLCs in the project
                                var plcSoftwares = plcBrowser.GetAllPlcSoftwares(project);

                                if (!plcSoftwares.Any())
                                {
                                    Console.WriteLine("No PLCs found in the project.");
                                    continue;
                                }

                                foreach (var plcSoftware in plcSoftwares)
                                {
                                    Console.WriteLine($"Processing PLC: {plcSoftware.Name}");

                                    // Create Export Directories
                                    string basePath = AppDomain.CurrentDomain.BaseDirectory;
                                    (string baseExportPath,string exportedDBsPath,string exportedCommentsPath, string _) =
                                        CreateExportDirectories(basePath, projectInfo.ProjectName, plcSoftware.Name);

                                    Console.WriteLine($"Export directory created: {baseExportPath}");

                                    // Export DBs
                                    //var foundDBs = dbBrowser.FindDBs(plcSoftware, dbKeys.Select(d => d.DBName).ToList());
                                    var foundDBs = dbBrowser.FindDBs(plcSoftware, dbKeys.Select(d => d.DBName).ToList());
                                    if (foundDBs.Any())
                                    {
                                        Console.WriteLine($"\nFound {foundDBs.Count} DBs");
                                        dbBrowser.ExportDBsToXml(foundDBs, exportedDBsPath);
                                    }
                                    else
                                    {
                                        Console.WriteLine("No DBs found to export in this PLC.");
                                    }
                                }
                            }
                            finally
                            {
                                project.Close();
                                Console.WriteLine($"Closed project: {projectInfo.ProjectName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing project {projectInfo.ProjectName}: {ex.Message}");
                        }
                    }
                }

                Console.WriteLine("\nPhase 1 complete: DB export finished.");

                // Step 5: Consolidate Comments
                Console.WriteLine("\n=== Consolidating Comments ===");
                string exportsBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports");
                var consolidatedComments = xmlExtractor.ProcessConsolidatedComments(exportsBasePath, dbListPath);

                if (consolidatedComments.Any())
                {
                    string completeAlarmListPath = Path.Combine(exportsBasePath, "CompleteAlarmList");
                    string outputFilePath = Path.Combine(completeAlarmListPath, "Consolidated_AlarmList.xlsx");

                    excelExporter.ExportComments(consolidatedComments, outputFilePath);
                    Console.WriteLine($"Consolidated alarm list exported to: {outputFilePath}");
                }
                else
                {
                    Console.WriteLine("No comments to consolidate into the alarm list.");
                }

                Console.WriteLine("\nProcessing completed. Press any key to exit.");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nAn error occurred: {ex.Message}");
            }
        }

        private static (string baseExportPath, string exportedDBsPath, string exportedCommentsPath, string completeAlarmListPath) CreateExportDirectories(
            string basePath, string selectedProjectName, string selectedPLC)
        {
            // Define the main Exports directory
            string exportsPath = Path.Combine(basePath, "Exports");

            // Ensure the Exports directory exists
            if (!Directory.Exists(exportsPath))
            {
                Directory.CreateDirectory(exportsPath);
                Console.WriteLine($"Created main Exports directory: {exportsPath}");
            }

            // Generate unique directory name for the current project and PLC
            string dateTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string exportDirectoryName = $"{selectedProjectName}_Exports_PLC-{selectedPLC}_{dateTime}";
            string baseExportPath = Path.Combine(exportsPath, exportDirectoryName);

            // Define subfolders for DBs, Comments, and CompleteAlarmList
            string exportedDBsPath = Path.Combine(baseExportPath, "ExportedDBs");
            string exportedCommentsPath = Path.Combine(baseExportPath, "ExportedComments");
            string completeAlarmListPath = Path.Combine(exportsPath, "CompleteAlarmList");

            // Create directories
            try
            {
                Directory.CreateDirectory(exportedDBsPath);
                Directory.CreateDirectory(exportedCommentsPath);

                if (!Directory.Exists(completeAlarmListPath))
                {
                    Directory.CreateDirectory(completeAlarmListPath);
                    Console.WriteLine($"Created CompleteAlarmList directory: {completeAlarmListPath}");
                }

                Console.WriteLine($"Created export directories:\n- DBs: {exportedDBsPath}\n- Comments: {exportedCommentsPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating directories: {ex.Message}");
                throw;
            }

            return (baseExportPath, exportedDBsPath, exportedCommentsPath, completeAlarmListPath);
        }

       
        private static void PrintAsciiArt()
        {
            string asciiArtFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pakak.txt");

            if (File.Exists(asciiArtFile))
            {
                try
                {
                    Console.WriteLine("\nHere’s something fun for you:");
                    //string art = File.ReadAllText(asciiArtFile);
                    //Console.WriteLine(art);
                    string[] artLines = File.ReadAllLines(asciiArtFile);

                    foreach (var line in artLines)
                    {
                        Console.WriteLine(line);
                        Thread.Sleep(50); // Delay of 200 milliseconds
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading ASCII art file: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("\nNo ASCII art found. Add an 'easter_egg.txt' file to the DBs folder!");
            }
        }

       
    }
}