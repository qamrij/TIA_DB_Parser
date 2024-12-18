using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using TiaDBReader.Models;
using System.Runtime.CompilerServices;
using System.Configuration;

namespace TiaDBReader
{
    public class XMLCommentExtractor
    {
        private XNamespace _namespace;


        public (List<CommentInfo> Comments, List<MissingCommentElement> MissingComments) ProcessConsolidatedComments(string exportsBasePath, string dbListPath)
        {
            var consolidatedComments = new List<CommentInfo>();
            var allMissingComments = new List<MissingCommentElement>();

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
                    var (comments, missingComments) = ExtractCommentsForSpecificDB(dbsDirectory, dbName, customKey);

                    if (comments.Any())
                    {
                        Console.WriteLine($"Extracted {comments.Count} comments for DB: {dbName}");
                        consolidatedComments.AddRange(comments);
                    }
                    else
                    {
                        Console.WriteLine($"No comments found for DB: {dbName}");
                    }
                    if (missingComments.Any())
                    {
                        Console.WriteLine($"Extracted {missingComments.Count}  missing comments for DB: {dbName}");
                        allMissingComments.AddRange(missingComments);
                    }
                    else
                    {
                        Console.WriteLine($"No  missing comments found for DB: {dbName}");
                    }

                }
            }

            return (consolidatedComments,allMissingComments);
        }


        private (List<CommentInfo> Comments, List<MissingCommentElement> MissingComments) ExtractCommentsForSpecificDB(string directoryPath, string dbName, int customKey)
        {
            // Logic to extract comments for a specific DB
            var (allComments,allMissingComments) = ExtractCommentsFromDirectory(directoryPath, new List<(string DBName, int CustomKey)> { (dbName, customKey) });
            return (allComments,allMissingComments);
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

        public (List<CommentInfo> Comments, List<MissingCommentElement> MissingComments) ExtractCommentsFromDirectory(string directoryPath, List<(string DBName, int CustomKey)> dbKeys)
        {
            var allComments = new List<CommentInfo>();
            var allMissingComments = new List<MissingCommentElement>();

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

                    var (commentsFromFile, missingCommentsFromFile) = ExtractCommentsFromFile(xmlPath, dbName, customKey);
                    allComments.AddRange(commentsFromFile);
                    allMissingComments.AddRange(missingCommentsFromFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing XML directory: {ex.Message}");
            }

            return (allComments,allMissingComments);
        }

        private (List<CommentInfo> Comments, List<MissingCommentElement> MissingComments) ExtractCommentsFromFile(string xmlPath, string dbName, int dbCustomKey)
        {
            var comments = new List<CommentInfo>();
            var missingComment = new List<MissingCommentElement>();

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
                    return (comments,missingComment);
                }

                foreach (var structName in structuresToFind)
                {
                    // Adjust dbCustomKey for "AlmShort" structure
                    int adjustedDbCustomKey = structName.Equals("AlmShort", StringComparison.OrdinalIgnoreCase)
                        ? dbCustomKey + 1
                        : dbCustomKey;
                    var (structureComments, structureMissingComments) = ProcessStructure(staticSection, structName, dbName, adjustedDbCustomKey);
                    comments.AddRange(structureComments);
                    missingComment.AddRange(structureMissingComments);
                }

                Console.WriteLine($"Found {comments.Count} comments in {dbName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing XML file {xmlPath}: {ex.Message}");
            }

            return (comments, missingComment);
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

        private (List<CommentInfo> Comments, List<MissingCommentElement> MissingComments) ProcessStructure(XElement staticSection, string structName, string dbName, int dbCustomKey)
        {
            var comments = new List<CommentInfo>();
            var missingComments = new List<MissingCommentElement>();

            // Determine SubGroupID based on structure name
            int baseSubGroupID = structName.Equals("Sign", StringComparison.OrdinalIgnoreCase) ? 5000 : 0;

            var mainStruct = staticSection.Elements(_namespace + "Member")
                                          .FirstOrDefault(m => m.Attribute("Name")?.Value == structName);

            if (mainStruct == null)
            {
                Console.WriteLine($"Structure {structName} not found in DB: {dbName}");
                return (comments,missingComments);
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
                var (subGroupComments,subGroupMissingComments) = ProcessSubGroup(subGroup, structName, dbName, dbCustomKey, subGroupID,ref globalNumber);
                comments.AddRange(subGroupComments);
                missingComments.AddRange(subGroupMissingComments);
                subGroupIndex++;
            }

            return (comments, missingComments);
        }

        private (List<CommentInfo> Comments,List<MissingCommentElement> MissingComments) ProcessSubGroup(XElement subGroup, string structName, string dbName, int dbCustomKey, int subGroupID, ref int globalNumber)
        {
            var comments = new List<CommentInfo>();
            var missingComments = new List<MissingCommentElement>();
            var subGroupName = subGroup.Attribute("Name")?.Value;

            if (string.IsNullOrEmpty(subGroupName))
                return (comments, missingComments);

            Console.WriteLine($"Processing subgroup: {subGroupName}");

            var members = subGroup.Elements(_namespace + "Sections")
                                  .Elements(_namespace + "Section")
                                  .Elements(_namespace + "Member")
                                  .Where(m => int.TryParse(m.Attribute("Name")?.Value, out _));

            foreach (var member in members)
            {
                var (comment,missingComment) = ExtractComment(member, dbName, dbCustomKey, structName, subGroupName, subGroupID,globalNumber);
                if (comment != null)
                {
                    comments.Add(comment);
                    globalNumber++;
                }
                if (missingComment != null) 
                {
                    missingComments.Add(missingComment);
                }
            }

            return (comments,missingComments);
        }

        
        private (CommentInfo, MissingCommentElement) ExtractComment(XElement member, string dbName, int dbCustomKey, string structName, string subGroupName, int subGroupID, int globalNumber)
        {
            try
            {
                var numberStr = member.Attribute("Name")?.Value;
                if (!int.TryParse(numberStr, out int number))
                    return (null,null);
                var commentText = member.Element(_namespace + "Comment")?
                                      .Elements(_namespace + "MultiLanguageText")
                                      .FirstOrDefault(x => x.Attribute("Lang")?.Value == "en-US")?
                                      .Value?.Trim();

                var customKey = dbCustomKey * 10000 + subGroupID + globalNumber;


                //generate the expected prefix of the comment
                var expectedPrefix = $"{ dbName}.{ structName}.{ subGroupName }.{ numberStr}";
                //alwayse generate the commentinfo object, if no commenttext put empty string
                var comment = new CommentInfo(dbName, structName, subGroupName, number, globalNumber, commentText??string.Empty, customKey);
                Console.WriteLine($"  {structName}.{subGroupName}.{number} - CustomKey: {customKey}: {commentText?? "No Comment"}");

                // Handle missing or mismatched comments
                if (string.IsNullOrEmpty(commentText))
                {
                    // Missing comment
                    var mismatch = "missing comment";
                    var missingComment = new MissingCommentElement(dbName, structName, subGroupName, numberStr,mismatch);
                    return (comment, missingComment);
                }
                else if (!StartsWithPrefix(commentText, expectedPrefix, out string wrongPrefix))
                {

                    // Mismatched comment
                    var mismatch = "mismatch detected";
                    var missingComment = new MissingCommentElement(dbName, structName, subGroupName, numberStr,mismatch,wrongPrefix);
                    return (comment, missingComment);
                }


                return (comment, null);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting comment: {ex.Message}");
                return (null,null);
            }
        }

        private bool StartsWithPrefix(string comment, string expectedPrefix,out string wrongPrefix)
        {
            var actualPrefix = comment.Split('>').FirstOrDefault()?.Trim();

            wrongPrefix = actualPrefix;
            
            return actualPrefix?.Equals(expectedPrefix, StringComparison.Ordinal) ?? false;
        }



    }
}