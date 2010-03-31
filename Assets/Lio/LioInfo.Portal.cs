using System.Collections.Generic;

namespace Assets
{
    public partial class LioInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Portal : LioInfo
        {
            /// <summary>
            /// 
            /// </summary>
            public struct PortalSettings
            {
                public int Frequency;
                public int DestinationWarpGroup;
                public string SkillLogic;
                public int DamageIgnoreTime;
                public int ReuseDelay;
                public int Gravity;

                public string PortalGfxBlobName;
                public string PortalGfxBlobId;
                public int PortalLightPermutation;
                public int PortalPaletteOffset;
                public int PortalHue;
                public int Saturation;
                public int PortalValue;
                public int PortalAnimationTime;

                public string RadarGfxBlobName;
                public string RadarGfxBlobId;
                public int RadarLightPermutation;
                public int RadarPaletteOffset;
                public int RadarHue;
                public int RadarSaturation;
                public int RadarValue;
                public int RadarAnimationTime;

                public string PortalSoundBlobName;
                public string PortalSoundBlobId;
                public int Simultaneous;
            }

            /// <summary>
            /// 
            /// </summary>
            public PortalSettings PortalData;

            /// <summary>
            /// 
            /// </summary>
            public Portal()
            {
                GeneralData.Type = Types.Portal;
            }

            /// <summary>
            /// Extracts properties for a Portal object from the CSV-formatted line.
            /// </summary>
            /// <param name="values">CSV-formatted line containing properties of a Portal object</param>
            public sealed override void ExtractCsvLine(List<string> values)
            {
                base.ExtractCsvLine(values);

                PortalData.Frequency = CSVReader.GetInt(values[10]);
                PortalData.DestinationWarpGroup = CSVReader.GetInt(values[11]);
                PortalData.SkillLogic = CSVReader.GetQuotedString(values[12]);
                PortalData.DamageIgnoreTime = CSVReader.GetInt(values[13]);
                PortalData.ReuseDelay = CSVReader.GetInt(values[14]);
                PortalData.Gravity = CSVReader.GetInt(values[15]);

                PortalData.PortalGfxBlobName = CSVReader.GetQuotedString(values[16]);
                PortalData.PortalGfxBlobId = CSVReader.GetQuotedString(values[17]);
                PortalData.PortalLightPermutation = CSVReader.GetInt(values[18]);
                PortalData.PortalPaletteOffset = CSVReader.GetInt(values[19]);
                PortalData.PortalHue = CSVReader.GetInt(values[20]);
                PortalData.Saturation = CSVReader.GetInt(values[21]);
                PortalData.PortalValue = CSVReader.GetInt(values[22]);
                PortalData.PortalAnimationTime = CSVReader.GetInt(values[23]);

                PortalData.RadarGfxBlobName = CSVReader.GetQuotedString(values[24]);
                PortalData.RadarGfxBlobId = CSVReader.GetQuotedString(values[25]);
                PortalData.RadarLightPermutation = CSVReader.GetInt(values[26]);
                PortalData.RadarPaletteOffset = CSVReader.GetInt(values[27]);
                PortalData.RadarHue = CSVReader.GetInt(values[28]);
                PortalData.RadarSaturation = CSVReader.GetInt(values[29]);
                PortalData.RadarValue = CSVReader.GetInt(values[30]);
                PortalData.RadarAnimationTime = CSVReader.GetInt(values[31]);

                PortalData.PortalSoundBlobName = CSVReader.GetQuotedString(values[32]);
                PortalData.PortalSoundBlobId = CSVReader.GetQuotedString(values[33]);
                PortalData.Simultaneous = CSVReader.GetInt(values[34]);
            }
        }
    }
}
