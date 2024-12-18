using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using TiaDBReader.Models;
using System.Runtime.CompilerServices;

namespace TiaDBReader
{
    public class XMLCommentExtractor
    {
        private XNamespace _namespace;


        public List<CommentInfo> ProcessConsolidatedComments(string exportsBasePath, string dbListPath)
        {
            var consolidatedComments = new List<CommentInfo>();

            // Parse DB list to retain order
            var dbList = ParseDBList(dbListPath);

            // Path to CompleteAlarmList folder
            string completeAlarmListPath = Path.Combine(exportsBasePath, "CompleteAlarmList");
            if (!Directory.Exists(completeAlarmListPath))
            {
                Directory.CreateDirectory(completeAlarmListPath);
                Console.WriteLine($"Created CompleteAlarmList folder: {completeAlarmListPath}");
            }

            // Get all ExportedDBs directories
            var exportedDBsDirectories = Directory.GetDirectories(exportsBasePath, "ExportedDBs", SearchOption.AllDirectories);

            foreach (var (dbName, customKey) in dbList)
            {
                foreach (var dbsDirectory in exportedDBsDirectories)
                {
                    Console.WriteLine($"Processing DB: {dbName} in directory: {dbsDirectory}");

                    // Extract comments for this specific DB
                    var comments = ExtractCommentsForSpecificDB(dbsDirectory, dbName, customKey);

                    if (comments.Any())
                    {
                        Console.WriteLine($"Extracted {comments.Count} comments for DB: {dbName}");
                        consolidatedComments.AddRange(comments);
                    }
                    else
                    {
                        Console.WriteLine($"No comments found for DB: {dbName}");
                    }
                }
            }

            return consolidatedComments;
        }


        private List<CommentInfo> ExtractCommentsForSpecificDB(string directoryPath, string dbName, int customKey)
        {
            // Logic to extract comments for a specific DB
            var allComments = ExtractCommentsFromDirectory(directoryPath, new List<(string DBName, int CustomKey)> { (dbName, customKey) });
            return allComments;
        }

        private List<(string DBName, int CustomKey)> ParseDBList(string dbListPath)
        {
            var dbList = new List<(string DBName, int CustomKey)>();

            if (!File.Exists(dbListPath))
            {
                Console.WriteLine($"DB list file not found: {dbListPath}");
                return dbList;
            }

            foreach (var line in File.ReadAllLines(dbListPath))
            {
                var parts = line.Split(',');

                if (parts.Length == 2 && int.TryParse(parts[1], out int customKey))
                {
                    dbList.Add((parts[0].Trim(), customKey));
                }
                else
                {
                    Console.WriteLine($"Invalid line in DB list file: {line}");
                }
            }

            return dbList;
        }

        public List<CommentInfo> ExtractCommentsFromDirectory(string directoryPath, List<(string DBName, int CustomKey)> dbKeys)
        {
            var allComments = new List<CommentInfo>();

            try
            {
                var xmlFiles = Directory.GetFiles(directoryPath, "*.xml");
                Console.WriteLine($"\nFound {xmlFiles.Length} XML files to process");

                foreach (var xmlPath in xmlFiles)
                {
                    var dbName = Path.GetFileNameWithoutExtension(xmlPath);

                    // Find the corresponding CustomKey for this DB
                    var dbKeyEntry = dbKeys.FirstOrDefault(entry => entry.DBName.Equals(dbName, StringComparison.OrdinalIgnoreCase));
                    if (dbKeyEntry == default)
                    {
                        Console.WriteLine($"No CustomKey found for DB: {dbName}, skipping...");
                        continue;
                    }

                    int customKey = dbKeyEntry.CustomKey;

                    Console.WriteLine($"\nProcessing XML for DB: {dbName} with CustomKey: {customKey}");

                    var commentsFromFile = ExtractCommentsFromFile(xmlPath, dbName, customKey);
                    allComments.AddRange(commentsFromFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing XML directory: {ex.Message}");
            }

            return allComments;
        }

        private List<CommentInfo> ExtractCommentsFromFile(string xmlPath, string dbName, int dbCustomKey)
        {
            var comments = new List<CommentInfo>();

            try
            {
                var doc = XDocument.Load(xmlPath);
                ExtractNamespace(doc);

                // Determine structures to find
                var structuresToFind = dbName.Equals("GLOBAL", StringComparison.OrdinalIgnoreCase)
                    ? new[] { "AlmLong", "AlmShort", "Sign" }
                    : new[] { "Alm", "Sign" };

                var staticSection = FindStaticSection(doc);
                if (staticSection == null)
                {
                    Console.WriteLine($"Warning: Static section not found in {dbName}");
                    return comments;
                }

                foreach (var structName in structuresToFind)
                {
                    // Adjust dbCustomKey for "AlmShort" structure
                    int adjustedDbCustomKey = structName.Equals("AlmShort", StringComparison.OrdinalIgnoreCase)
                        ? dbCustomKey + 1
                        : dbCustomKey;
                    var structureComments = ProcessStructure(staticSection, structName, dbName, adjustedDbCustomKey);
                    comments.AddRange(structureComments);
                }

                Console.WriteLine($"Found {comments.Count} comments in {dbName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing XML file {xmlPath}: {ex.Message}");
            }

            return comments;
        }


        private void ExtractNamespace(XDocument doc)
        {
            try
            {
                // Cerca il tag Sections e estrai il suo namespace
                var sectionsElement = doc.Descendants("Sections").FirstOrDefault();
                _namespace = sectionsElement?.Attribute("xmlns")?.Value != null
                    ? XNamespace.Get(sectionsElement.Attribute("xmlns").Value)
                    : XNamespace.Get("http://www.siemens.com/automation/Openness/SW/Interface/v5");

                Console.WriteLine($"Using namespace: {_namespace}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting namespace: {ex.Message}");
                // Usa il namespace di default in caso di errore
                _namespace = XNamespace.Get("http://www.siemens.com/automation/Openness/SW/Interface/v5");
            }
        }

        private XElement FindStaticSection(XDocument doc)
        {
            return doc.Descendants("Interface")
                     .Elements(_namespace + "Sections")
                     .Elements(_namespace + "Section")
                     .FirstOrDefault(s => s.Attribute("Name")?.Value == "Static");
        }

        private List<CommentInfo> ProcessStructure(XElement staticSection, string structName, string dbName, int dbCustomKey)
        {
            var comments = new List<CommentInfo>();

            // Determine SubGroupID based on structure name
            int baseSubGroupID = structName.Equals("Sign", StringComparison.OrdinalIgnoreCase) ? 5000 : 0;

            var mainStruct = staticSection.Elements(_namespace + "Member")
                                          .FirstOrDefault(m => m.Attribute("Name")?.Value == structName);

            if (mainStruct == null)
            {
                Console.WriteLine($"Structure {structName} not found in DB: {dbName}");
                return comments;
            }

            Console.WriteLine($"Processing structure: {structName}");

            var subGroups = mainStruct.Elements(_namespace + "Sections")
                                      .Elements(_namespace + "Section")
                                      .Elements(_namespace + "Member");
            int globalNumber = 0; // Running counter for numbers within the structure
            int subGroupIndex = 0;
            foreach (var subGroup in subGroups)
            {
                int subGroupID = baseSubGroupID; //+ subGroupIndex; // Incremental SubGroupID
                var subGroupComments = ProcessSubGroup(subGroup, structName, dbName, dbCustomKey, subGroupID,ref globalNumber);
                comments.AddRange(subGroupComments);
                subGroupIndex++;
            }

            return comments;
        }

        private List<CommentInfo> ProcessSubGroup(XElement subGroup, string structName, string dbName, int dbCustomKey, int subGroupID, ref int globalNumber)
        {
            var comments = new List<CommentInfo>();
            var subGroupName = subGroup.Attribute("Name")?.Value;

            if (string.IsNullOrEmpty(subGroupName))
                return comments;

            Console.WriteLine($"Processing subgroup: {subGroupName}");

            var members = subGroup.Elements(_namespace + "Sections")
                                  .Elements(_namespace + "Section")
                                  .Elements(_namespace + "Member")
                                  .Where(m => int.TryParse(m.Attribute("Name")?.Value, out _));

            foreach (var member in members)
            {
                var comment = ExtractComment(member, dbName, dbCustomKey, structName, subGroupName, subGroupID,globalNumber);
                if (comment != null)
                {
                    comments.Add(comment);
                    globalNumber++;
                }
            }

            return comments;
        }

        private CommentInfo ExtractComment(XElement member, string dbName, int dbCustomKey, string structName, string subGroupName, int subGroupID, int globalNumber)
        {
            try
            {
                var numberStr = member.Attribute("Name")?.Value;
                if (!int.TryParse(numberStr, out int number))
                    return null;
                var commentText = member.Element(_namespace + "Comment")?
                                      .Elements(_namespace + "MultiLanguageText")
                                      .FirstOrDefault(x => x.Attribute("Lang")?.Value == "en-US")?
                                      .Value?.Trim();

                if (string.IsNullOrEmpty(commentText))
                    return null;

                var customKey = dbCustomKey *10000 + subGroupID + globalNumber;

                Console.WriteLine($"  {structName}.{subGroupName}.{number} - CustomKey: {customKey}: {commentText}");

                // Create and return the CommentInfo object
                return new CommentInfo(dbName,structName,subGroupName,number,globalNumber,commentText,customKey);

                /*
                return new CommentInfo
                {
                    DB = dbName,
                    Structure = structName,
                    SubGroup = subGroupName,
                    Number = number,
                    Comment = commentText,
                    CustomKey = customKey // Assign the calculated CustomKey
                };*/
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting comment: {ex.Message}");
                return null;
            }
        }
    }
}