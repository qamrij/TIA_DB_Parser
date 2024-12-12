using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering;
using TiaDBReader.Models;

namespace TiaDBReader
{
    public class DBBrowser
    {
        public List<(string DBName, int CustomKey)> ReadDBNamesWithKeysFromFile(string filePath)
        {
            var dbs = new List<(string, int)>();
            try
            {
                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(',');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int customKey))
                        {
                            dbs.Add((parts[0].Trim(), customKey));
                        }
                        else
                        {
                            Console.WriteLine($"Invalid entry in DB file: {line}");
                        }
                    }
                    Console.WriteLine("\nDB names and CustomKeys loaded from file:");
                    foreach (var (dbName, customKey) in dbs)
                    {
                        Console.WriteLine($"DB: {dbName}, CustomKey: {customKey}");
                    }
                }
                else
                {
                    Console.WriteLine("File not found!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
            }
            return dbs;
        }

        public List<DBInfo> FindDBs(PlcSoftware plc, List<string> dbNames)
        {
            var foundDBs = new List<DBInfo>();

            try
            {
                Console.WriteLine("\nSearching for DBs...");
                foreach (var dbName in dbNames)
                {
                    var db = SearchDBInGroups(plc.BlockGroup, dbName);

                    if (db != null)
                    {
                        var dbInfo = new DBInfo(db);
                        foundDBs.Add(dbInfo);
                        Console.WriteLine($"Found {dbInfo.DBType} DB: {dbInfo.DBName} (UDT: {dbInfo.UDTName})");
                    }
                    else
                    {
                        Console.WriteLine($"DB {dbName} not found");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding DBs: {ex.Message}");
            }

            return foundDBs;
        }

        private PlcBlock SearchDBInGroups(PlcBlockGroup parentGroup, string dbName)
        {
            // Cerca nei blocchi del gruppo corrente
            var block = parentGroup.Blocks.FirstOrDefault(b =>
                b.Name.Equals(dbName, StringComparison.OrdinalIgnoreCase));

            if (block != null)
                return block;

            // Se non trovato, cerca ricorsivamente in tutti i sottogruppi
            foreach (var group in parentGroup.Groups)
            {
                block = SearchDBInGroups(group, dbName);
                if (block != null)
                    return block;
            }

            return null;
        }

        public void ExportDBsToXml(List<DBInfo> dbs, string outputPath)
        {
            try
            {
               
                if (!Directory.Exists(outputPath))
                {
                    Console.WriteLine($"Creating directory: {outputPath}");
                    Directory.CreateDirectory(outputPath);
                }

                foreach (var db in dbs)
                {
                    Console.WriteLine($"\nExporting DB: {db.DBName}");
                    string xmlPath = Path.Combine(outputPath, $"{db.DBName}.xml");
                    FileInfo xmlFile = new FileInfo(xmlPath);

                    try
                    {
                        Console.WriteLine($"Exporting {db.DBName} to XML...");
                        db.Block.Export(xmlFile, ExportOptions.WithDefaults);

                        if (xmlFile.Exists)
                        {
                            Console.WriteLine($"Successfully exported to: {xmlFile.FullName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error exporting DB {db.DBName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExportDBsToXml: {ex.Message}");
            }
        }
    }
}