namespace Assets
{
    public abstract partial class VehInfo : ICsvParseable
    {
        public sealed class Nested : VehInfo
        {
            public string VehicleFileName;

            public Nested()
            {
                Type = Types.Nested;
            }

            public override void Parse(ICsvParser parser)
            {
                this.Version = parser.GetInt('v');
                this.Id = parser.GetInt();
                this.Name = parser.GetString();
                this.VehicleFileName = parser.GetString();
            }
        }
    }
}
