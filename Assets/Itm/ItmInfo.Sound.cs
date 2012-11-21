using System.Collections.Generic;
using System;
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
                if (blobID == "2")
                    Console.Write("Found blob id '{0}', {1},{2},{3} ",blobName, blobName, blobID, maxSimultaneous);
            }

        }
    }
}
