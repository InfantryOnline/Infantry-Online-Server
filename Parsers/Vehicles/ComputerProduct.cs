namespace Parsers
{
    public abstract partial class Vehicle : ICsvFormat
    {
        /// <summary>
        /// Class inside Computer type.
        /// </summary>
        public class ComputerProduct // 00422CD5
        {
            public string Title { get; set; } // 80
            public short ProductToCreate { get; set; } // B8
            public short Quantity { get; set; } // BA
            public int Team { get; set; } // C4
            public int Time { get; set; } // A8
            public short PlayerItemNeededId { get; set; } // BC
            public int PlayerItemNeededQuantity { get; set; } // AC
            public int Cost { get; set; } // B0
            public string SkillLogic { get; set; } // 00
            public short TeamItemNeededId { get; set; } // BE
            public short TeamItemNeededQuantity { get; set; } // B4
            public string TeamBuildingLogic { get; set; } // 40
            public int TeamQueueRequest { get; set; } // C8
            public short Confirm { get; set; } // C0

            public void Read(int version, ICsvReader reader)
            {
                this.Title = reader.GetString();
                this.ProductToCreate = reader.GetShort();
                this.Quantity = reader.GetShort();
                this.Team = reader.GetInt();
                this.Time = reader.GetInt();
                this.PlayerItemNeededId = reader.GetShort();
                this.PlayerItemNeededQuantity = reader.GetInt();
                this.Cost = reader.GetInt();
                this.SkillLogic = reader.GetString();
                this.TeamItemNeededId = reader.GetShort();
                this.TeamItemNeededQuantity = reader.GetShort();
                this.TeamBuildingLogic = reader.GetString();
                this.TeamQueueRequest = reader.GetInt();
                this.Confirm = (version >= 51) ? reader.GetShort() : (short)0;
            }
        }
    }
}
