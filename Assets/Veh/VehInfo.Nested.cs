namespace Assets
{
    public abstract partial class VehInfo : ICsvParseable
    {
        /// <summary>
        /// jello
        /// </summary>
        public sealed class Nested : VehInfo
        {
            /*public int Unknown434;
            public string Unknown3D4;*/
            public string Unknown5C8;

            public Nested()
            {
                Type = Types.Nested;
            }

            public override void Parse(ICsvParser parser)
            {
                base.Parse(parser);
                this.Version = int.Parse(parser.GetString().Substring(1)); // sets version...?
                this.Id = parser.GetInt();
                this.Name = parser.GetString();
                this.Unknown5C8 = parser.GetString();
            }
        }
    }
}
