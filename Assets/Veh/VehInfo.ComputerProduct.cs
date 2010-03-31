namespace Assets
{
    public abstract partial class VehInfo : ICsvParseable
    {
        /// <summary>
        /// Class inside Computer type.
        /// </summary>
        public class ComputerProduct // 00422CD5
        {
            public string Title; // 80
            public short ProductToCreate; // B8
            public short Quantity; // BA
            public int Team; // C4
            public int Time; // A8
            public short PlayerItemNeededId; // BC
            public int PlayerItemNeededQuantity; // AC
            public int Cost; // B0
            public string SkillLogic; // 00
            public short TeamItemNeededId; // BE
            public short TeamItemNeededQuantity; // B4
            public string TeamBuildingLogic; // 40
            public int TeamQueueRequest; // C8
            public short Confirm; // C0

            public void Parse(int version, ICsvParser parser)
            {
                this.Title = parser.GetString();
                this.ProductToCreate = parser.GetShort();
                this.Quantity = parser.GetShort();
                this.Team = parser.GetInt();
                this.Time = parser.GetInt();
                this.PlayerItemNeededId = parser.GetShort();
                this.PlayerItemNeededQuantity = parser.GetInt();
                this.Cost = parser.GetInt();
                this.SkillLogic = parser.GetQuotedString();
                this.TeamItemNeededId = parser.GetShort();
                this.TeamItemNeededQuantity = parser.GetShort();
                this.TeamBuildingLogic = parser.GetQuotedString();
                this.TeamQueueRequest = parser.GetInt();
                this.Confirm = (version >= 51) ? parser.GetShort() : (short)0;
            }
        }
    }
}
