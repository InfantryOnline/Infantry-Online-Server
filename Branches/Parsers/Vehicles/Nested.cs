namespace Parsers
{
    public abstract partial class Vehicle : ICsvFormat
    {
        /// <summary>
        /// jello
        /// </summary>
        public sealed class Nested : Vehicle
        {
            /*public int Unknown434;
            public string Unknown3D4;*/
            public string Unknown5C8 { get; set; }

            public override void Read(ICsvReader reader)
            {
                base.Read(reader);
                this.Version = int.Parse(reader.GetString().Substring(1)); // sets version...?
                this.Id = reader.GetInt();
                this.Name = reader.GetString();
                this.Unknown5C8 = reader.GetString();
            }
        }
    }
}
