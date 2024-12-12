using System;
using System.Linq;
using TiaDBReader.Models;
using System.IO;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using System.Threading;

namespace TiaDBReader
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Initialize helper classes
                var tiaConnection = new TiaPortalConnection();
                var plcBrowser = new PlcBrowser();
                var dbBrowser = new DBBrowser();
                var xmlExtractor = new XMLCommentExtractor();
                var excelExporter = new ExcelExporter();

                // Connect to TIA Portal
                var tiaPortal = tiaConnection.Connect();
                if (tiaPortal == null)
                {
                    Console.WriteLine("Failed to connect to TIA Portal");
                    return;
                }

                // Get the current project
                var project = tiaConnection.GetCurrentProject();
                if (project == null)
                {
                    Console.WriteLine("No project found");
                    return;
                }

                // Get and select the PLC
                var plcSoftwares = plcBrowser.GetAllPlcSoftwares(project).ToList();
                var selectedPlc = plcBrowser.SelectPLC(plcSoftwares);

                if (selectedPlc != null)
                {

                    string basePath = AppDomain.CurrentDomain.BaseDirectory;
                    string selectedProjectName = project.Name;
                    string selectedPLCName = selectedPlc.Name;

                    // Create export directories
                    var (baseExportPath, exportedDBsPath, exportedCommentsPath) = CreateExportDirectories(basePath, selectedProjectName, selectedPLCName);

                    Console.WriteLine($"Export directory created: {baseExportPath}");



                    Console.WriteLine($"Using base path: {basePath}");
                    string dbListPath = Path.Combine(basePath, "DBs", "DBs.txt");

                    // Phase 1: Read DB names and CustomKeys from file
                    Console.WriteLine("\n=== Phase 1: Load DB List ===");
                    var dbKeys = dbBrowser.ReadDBNamesWithKeysFromFile(dbListPath);

                    if (!dbKeys.Any())
                    {
                        Console.WriteLine("No DB names or CustomKeys loaded from file");
                        return;
                    }

                    // Phase 2: Search and export DBs
                    Console.WriteLine("\n=== Phase 2: DB Search and Export ===");
                    var foundDBs = dbBrowser.FindDBs(selectedPlc, dbKeys.Select(d => d.DBName).ToList());
                    if (foundDBs.Any())
                    {
                        Console.WriteLine($"\nFound {foundDBs.Count} DBs");
                        dbBrowser.ExportDBsToXml(foundDBs, exportedDBsPath);
                    }
                    else
                    {
                        Console.WriteLine("No DBs found to export");
                        return;
                    }

                    // Phase 3: Extract comments
                    Console.WriteLine("\n=== Phase 3: Comment Extraction ===");
                    var comments = xmlExtractor.ExtractCommentsFromDirectory(exportedDBsPath, dbKeys);
                    Console.WriteLine($"\nTotal comments extracted: {comments.Count}");

                    // Phase 4: Export to Excel
                    if (comments.Any())
                    {
                        Console.WriteLine("\n=== Phase 4: Excel Export ===");
                        excelExporter.ExportComments(comments, exportedCommentsPath);
                    }
                    else
                    {
                        Console.WriteLine("\nNo comments to export to Excel");
                    }
                }
                else 
                {
                    // Print ASCII art or fallback message
                    PrintAsciiArt();
                }

                // Clean up resources
                tiaConnection.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nAn error occurred: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                
                
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
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

        private static (string baseExportPath, string exportedDBsPath, string exportedCommentsPath) CreateExportDirectories(
                                                                                                                            string basePath,
                                                                                                                            string selectedProjectName,
                                                                                                                            string selectedPLC)
        {
            // Generate unique directory name
            string dateTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string exportDirectoryName = $"{selectedProjectName}_Exports_PLC-{selectedPLC}_{dateTime}";
            string baseExportPath = Path.Combine(basePath, exportDirectoryName);

            // Define subfolders
            string exportedDBsPath = Path.Combine(baseExportPath, "ExportedDBs");
            string exportedCommentsPath = Path.Combine(baseExportPath, "ExportedComments");

            // Create directories
            try
            {
                Directory.CreateDirectory(exportedDBsPath);
                Directory.CreateDirectory(exportedCommentsPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating directories: {ex.Message}");
                throw;
            }

            // Return paths
            return (baseExportPath, exportedDBsPath, exportedCommentsPath);
        }

    }
}