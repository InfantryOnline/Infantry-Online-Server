namespace Assets
{
    public abstract partial class VehInfo : ICsvParseable
    {
        /// <summary>
        /// jello
        /// </summary>
        public sealed class Nested : VehInfo
        {
            public string VehicleFileName;

            public Nested()
            {
                Type = Types.Nested;
            }

            public override void Parse(ICsvParser parser)
            {
                this.Version = int.Parse(parser.GetString().Substring(1)); // sets version...?
                this.Id = parser.GetInt();
                this.Name = parser.GetString();
                this.VehicleFileName = parser.GetString();
            }
        }
    }
}
