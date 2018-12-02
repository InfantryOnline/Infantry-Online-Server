namespace Parsers
{
    public interface ICsvFormat
    {
        void Read(ICsvReader reader);
    }
}
