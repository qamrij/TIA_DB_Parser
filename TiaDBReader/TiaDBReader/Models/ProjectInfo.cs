using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiaDBReader.Models
{
    public class ProjectInfo
    {
        public string ProjectPath { get; set; }
        public string ProjectName { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; }
    }
}
