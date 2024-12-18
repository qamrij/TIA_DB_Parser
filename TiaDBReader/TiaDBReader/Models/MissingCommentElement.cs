using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiaDBReader.Models
{
    public class MissingCommentElement
    {
        public string DBName { get; set; }
        public string Structure { get; set; }
        public string SubGroup { get; set; }
        public string ElementName { get; set; }

        public string Mismatch { get; set; }
        public string WrongPrefix { get; set; }

        public MissingCommentElement(string dbName, string structure, string subGroup, string elementName,string mismatch = "", string wrongPrefix = "")
        {
            DBName = dbName;
            Structure = structure;
            SubGroup = subGroup;
            ElementName = elementName;
            Mismatch = string.IsNullOrEmpty(mismatch) ? string.Empty : mismatch;
            WrongPrefix = string.IsNullOrEmpty(wrongPrefix) ? string.Empty : wrongPrefix;
        }
    }
}
