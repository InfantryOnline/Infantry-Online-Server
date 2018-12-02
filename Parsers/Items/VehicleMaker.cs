namespace Parsers
{
    public abstract partial class Item : ICsvFormat
    {
        public sealed class VehicleMaker : Use
        {
            public int Unknown260 { get; set; }

            public override void Read(ICsvReader reader)
            {
                base.Read(reader);
                this.Unknown260 = reader.GetInt();
            }
        }
    }
}
