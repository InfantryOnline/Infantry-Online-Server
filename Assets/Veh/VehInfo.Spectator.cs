namespace Assets
{
    public abstract partial class VehInfo : ICsvParseable
    {
        public sealed class Spectator : VehInfo
        {
            public Spectator()
            {
                Type = Types.Spectator;
            }
        }
    }
}
