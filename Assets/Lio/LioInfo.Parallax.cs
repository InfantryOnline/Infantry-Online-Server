using System.Collections.Generic;

namespace Assets
{
    public partial class LioInfo
    {
        public sealed class Parallax : LioInfo
        {
            public struct ParallaxSettings
            {
                public int NearDistance;
                public int FarDistance;
                public int Quantity;
                public string ParallaxBlobName;
                public string ParallaxBlobId;
                public int LightPermutation;
                public int PaletteOffset;
                public int Hue;
                public int Saturation;
                public int Value;
                public int AnimationTime;
            }

            /// <summary>
            /// 
            /// </summary>
            public ParallaxSettings ParallaxData;
            
            /// <summary>
            /// 
            /// </summary>
            public Parallax()
            {
                GeneralData.Type = Types.Parallax;
            }

            /// <summary>
            /// Extracts properties for a Parallax object from the CSV-formatted line.
            /// </summary>
            /// <param name="values">CSV-formatted line containing properties of a Parallax object</param>
            public sealed override void ExtractCsvLine(List<string> values)
            {
                base.ExtractCsvLine(values);

                ParallaxData.NearDistance = CSVReader.GetInt(values[10]);
                ParallaxData.FarDistance = CSVReader.GetInt(values[11]);
                ParallaxData.Quantity = CSVReader.GetInt(values[12]);
                ParallaxData.ParallaxBlobName = CSVReader.GetQuotedString(values[13]);
                ParallaxData.ParallaxBlobId = CSVReader.GetQuotedString(values[14]);
                ParallaxData.LightPermutation = CSVReader.GetInt(values[15]);
                ParallaxData.PaletteOffset = CSVReader.GetInt(values[16]);
                ParallaxData.Hue = CSVReader.GetInt(values[17]);
                ParallaxData.Saturation = CSVReader.GetInt(values[18]);
                ParallaxData.Value = CSVReader.GetInt(values[19]);
                ParallaxData.AnimationTime = CSVReader.GetInt(values[20]);
            }
        }
    }
}
