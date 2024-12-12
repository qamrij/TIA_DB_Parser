using Siemens.Engineering.SW.Blocks;
using System;
using System.Linq;

namespace TiaDBReader.Models
{
    public class DBInfo
    {
        public PlcBlock Block { get; }
        public string DBName { get; }
        public string UDTName { get; }
        public string DBType { get; }

        public DBInfo(PlcBlock block)
        {
            Block = block ?? throw new ArgumentNullException(nameof(block));
            DBName = block.Name;
            DBType = DetermineDBType(block);
            UDTName = GenerateUDTName(DBName);
        }

        private string DetermineDBType(PlcBlock block)
        {
            if (block is GlobalDB)
                return "Global";
            if (block is ArrayDB)
                return "Array";
            if (block is InstanceDB)
                return "Instance";
            return "Unknown";
        }

        private string GenerateUDTName(string dbName)
        {
            string baseName = new string(dbName.Where(c => !char.IsDigit(c)).ToArray());
            return baseName == "GLOBAL" ? "GLOBAL_Alm" : $"{baseName}_Alm";
        }
    }
}