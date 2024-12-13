using System;

namespace TiaDBReader.Models
{
    public class CommentInfo
    {
        /*
        public string DB { get; set; }
        public string Structure { get; set; }
        public string SubGroup { get; set; }
        public int Number { get; set; }
        public string Comment { get; set; }
        public int CustomKey { get; set; }
        */

        // Properties 
        public string DB { get; set; }
        public string Structure { get; set; }
        public string SubGroup { get; set; }
        public int Number { get; set; }
        public string Name { get; private set; } // Concatenation of DB, Structure, SubGroup, and Number
        public string Tag { get; private set; } // Format: "HMI_<DB>_<Structure>"
        public int Value { get; private set; } // Same as globalNumber
        public string Message { get; set; } // Extracted comment text
        public int Priority { get; private set; } // 1 if Structure = "Signal", else 0
        public int CustomKey { get; set; } // Unique identifier

        // Static Properties
        public static string Folder { get; } = "";
        public static string ActivationType { get; } = "Bit";
        public static string AlarmType { get; } = "SimpleEvent";
        public static string PageType { get; } = "";
        public static string Page { get; } = "";
        public static string IsLogged { get; } = "True";//"VERO";
        public static string IsPrinted { get; } = "True";//"VERO";
        public static string HasAcknowledgeTag { get; } = "False";//"FALSO";
        public static string AcknowledgeType { get; } = "Equal";
        public static string AcknowledgeTag { get; } = "";
        public static int AcknowledgeValue { get; } = 0;
        public static string RangeMin { get; } = "";
        public static string RangeMax { get; } = "";
        public static string SingleInstance { get; } = "False";//"FALSO";
        public static string NegativeLogic { get; } = "False";//"FALSO";
        public static string TagOPC { get; } = "False";//"FALSO";
        public static string OPCFolder { get; } = "ESA";
        public static string Identifier { get; } = "";
        public static int NodeId { get; } = 0;
        public static string NumPath { get; } = "";

        // Constructor
        public CommentInfo(string dbName, string structName, string subGroupName, int number,int globalNumber, string commentText, int customKey)
        {
            // Dynamic Properties
            DB = dbName;
            Structure = structName;
            SubGroup = subGroupName;
            Number = number;
            Name = $"{DB}.{Structure}.{SubGroup}.{Number}";
            Tag = $"HMI_{DB}.{Structure}";
            Value = globalNumber;

            // Extract part of the comment after '>'
            if (!string.IsNullOrEmpty(commentText) && commentText.Contains(">"))
            {
                var parts = commentText.Split('>');
                Message = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            }
            else
            {
                Message = string.Empty; // No '>' found or empty comment
            }
            Priority = structName.Equals("Sign", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            CustomKey = customKey;

            // Static properties are already defined as static readonly
        }


    }
}