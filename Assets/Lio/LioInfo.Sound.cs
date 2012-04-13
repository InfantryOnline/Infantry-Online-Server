using System.Collections.Generic;

namespace Assets
{
    public partial class LioInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Sound : LioInfo
        {
            /// <summary>
            /// 
            /// </summary>
            public struct SoundSettings
            {
                public int Frequency;
                public int PlayOdds;
                public bool IsTriggeredOnEntry;
                public int TriggerDelay;
                public int SoundVolume;
                public int MinPlayerCount;
                public int MaxPlayerCount;
                public int InactiveFrame;
                public string SoundGfxBlobName;
                public string SoundGfxBlobId;
                public int LightPermutation;
                public int PaletteOffset;
                public int Hue;
                public int Saturation;
                public int Value;
                public int AnimationTime;
                public string SoundBlobName;
                public string SoundBlobId;
                public int Simultaneous;
            }

            /// <summary>
            /// 
            /// </summary>
            public SoundSettings SoundData;

            /// <summary>
            /// 
            /// </summary>
            public Sound()
            {
                GeneralData.Type = Types.Sound;
            }

            /// <summary>
            /// Extracts properties for a Sound object from the CSV-formatted line.
            /// </summary>
            /// <param name="values">CSV-formatted line containing properties of a Sound object</param>
            public sealed override void ExtractCsvLine(List<string> values)
            {
                base.ExtractCsvLine(values);

                SoundData.Frequency = CSVReader.GetInt(values[10]);
                SoundData.PlayOdds = CSVReader.GetInt(values[11]);
                SoundData.IsTriggeredOnEntry = CSVReader.GetBool(values[12]);
                SoundData.TriggerDelay = CSVReader.GetInt(values[13]);
                SoundData.SoundVolume = CSVReader.GetInt(values[14]);
                SoundData.MinPlayerCount = CSVReader.GetInt(values[15]);
                SoundData.MaxPlayerCount = CSVReader.GetInt(values[16]);
                SoundData.InactiveFrame = CSVReader.GetInt(values[17]);
                SoundData.SoundGfxBlobName = CSVReader.GetQuotedString(values[18]);
                SoundData.SoundGfxBlobId = CSVReader.GetQuotedString(values[19]);
                SoundData.LightPermutation = CSVReader.GetInt(values[20]);
                SoundData.PaletteOffset = CSVReader.GetInt(values[21]);
                SoundData.Hue = CSVReader.GetInt(values[22]);
                SoundData.Saturation = CSVReader.GetInt(values[23]);
                SoundData.Value = CSVReader.GetInt(values[24]);
                SoundData.AnimationTime = CSVReader.GetInt(values[25]);
                SoundData.SoundBlobName = CSVReader.GetQuotedString(values[26]);
                SoundData.SoundBlobId = CSVReader.GetQuotedString(values[27]);
                SoundData.Simultaneous = CSVReader.GetInt(values[28]);

                //Load the blobs
                BlobsToLoad.Add(SoundData.SoundBlobName);
                BlobsToLoad.Add(SoundData.SoundGfxBlobName);
            }
        }
    }
}
