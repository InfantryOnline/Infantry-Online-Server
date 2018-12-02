namespace Parsers
{
    public interface ICsvReader
    {
        bool AtEnd { get; }

        /// <summary>
        /// Skip a single token
        /// </summary>
        void Skip();

        /// <summary>
        /// Skip tokens
        /// </summary>
        /// <param name="count">How many tokens to skip</param>
        void Skip(int count);

        // Basic
        bool GetBool();
        sbyte GetSByte();
        short GetShort();
        int GetInt();
        int GetInt(params char[] trim);
        float GetFloat();
        string GetString();
        void GetInstance<TInstanceType>(ref TInstanceType value)
            where TInstanceType : class, ICsvFormat;
        TInstanceType GetInstance<TInstanceType>()
            where TInstanceType : class, ICsvFormat, new();

        // Lists
        short[] GetShorts(int count);
        int[] GetInts(int count);
        TInstanceType[] GetInstances<TInstanceType>(int count)
            where TInstanceType : class, ICsvFormat, new();
    }
}
