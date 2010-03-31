using System.Collections.Generic;

namespace Assets
{
    /// <summary>
    /// 
    /// </summary>
    public partial class SkillInfo : ICsvParseable
    {
        /// <summary>
        /// 
        /// </summary>
        enum Resets
        {
            /// <summary>
            /// 
            /// </summary>
            NoSkills = 0,

            /// <summary>
            /// 
            /// </summary>
            AllSkills = 1,

            /// <summary>
            /// 
            /// </summary>
            OnlySkills = 2,

            /// <summary>
            /// 
            /// </summary>
            OnlyAttributes = 3
        }

        public int Version;
        public int SkillId;
        public int Price;
        public int DefaultVehicleId;
        public int ResetSkills;
        public bool ResetInventory;
        public int CashAdjustment;
        public string Name;
        public string Category;
        public string Logic;
        public string Description;
        public string BlobName;
        public string BlobId;
        public List<InventoryMutator> InventoryMutators = new List<InventoryMutator>();

        public void Parse(ICsvParser parser)
        {
            Version = parser.GetInt('v');
            SkillId = parser.GetInt();
            Price = parser.GetInt();
            DefaultVehicleId = parser.GetInt();
            ResetSkills = parser.GetInt();
            ResetInventory = parser.GetBool();
            InventoryMutator[] mutators = parser.GetInstances<InventoryMutator>(16);
            CashAdjustment = parser.GetInt();
            Name = parser.GetString();
            Category = parser.GetString();
            Logic = parser.GetString();
            Description = parser.GetString();
            BlobName = parser.GetString();
            BlobId = parser.GetString();

            while(!parser.AtEnd)
            {
                parser.Skip();
            }

            InventoryMutators.AddRange(mutators);
        }

        static public List<SkillInfo> Load(string filename)
        {
            return CsvFile<SkillInfo>.Load(filename, delegate() { return new SkillInfo(); });
        }
    }
}
