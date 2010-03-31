namespace Assets
{
    public partial class SkillInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public class InventoryMutator : ICsvParseable
        {
            /// <summary>
            /// 
            /// </summary>
            public int ItemId;

            /// <summary>
            /// 
            /// </summary>
            public int Quantity;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parser"></param>
            public void Parse(ICsvParser parser)
            {
                ItemId = parser.GetInt();
                Quantity = parser.GetInt();
            }
        }
    }
}
