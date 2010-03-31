namespace Assets
{
    public interface ICsvParser
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
        string GetQuotedString();
        void GetInstance<TInstanceType>(ref TInstanceType value)
            where TInstanceType : class, ICsvParseable;
        TInstanceType GetInstance<TInstanceType>()
            where TInstanceType : class, ICsvParseable, new();

        // Lists
        short[] GetShorts(int count);
        int[] GetInts(int count);
        TInstanceType[] GetInstances<TInstanceType>(int count)
            where TInstanceType : class, ICsvParseable, new();
    }
}
