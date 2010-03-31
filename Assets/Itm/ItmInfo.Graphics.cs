using System.Collections.Generic;

namespace Assets
{
    public partial class ItemInfo
    {
        public class Graphics
        {
            public string blobName;
            public string blobID;
            public int lightPermutation;
            public int paletteOffset;
            public int hue;
            public int saturation;
            public int value;
            public int animationTime;

            public Graphics(ref List<string> values, int start)
            {
                blobName = CSVReader.GetString(values[start]);
                blobID = CSVReader.GetString(values[start + 1]);
                lightPermutation = CSVReader.GetInt(values[start + 2]);
                paletteOffset = CSVReader.GetInt(values[start + 3]);
                hue = CSVReader.GetInt(values[start + 4]);
                saturation = CSVReader.GetInt(values[start + 5]);
                value = CSVReader.GetInt(values[start + 6]);
                animationTime = CSVReader.GetInt(values[start + 7]);
            }

        }
    }
}
