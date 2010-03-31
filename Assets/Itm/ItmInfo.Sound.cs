using System.Collections.Generic;

namespace Assets
{
    public partial class ItemInfo
    {
        public class Sound
        {
            public string blobName;
            public string blobID;
            public int maxSimultaneous;

            public Sound(ref List<string> values, int start)
            {
                blobName = CSVReader.GetString(values[start]);
                blobID = CSVReader.GetString(values[start + 1]);
                maxSimultaneous = CSVReader.GetInt(values[start + 2]);
            }

        }
    }
}
