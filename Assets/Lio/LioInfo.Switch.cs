using System.Collections.Generic;

namespace Assets
{
    public partial class LioInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Switch : LioInfo
        {
            public struct SwitchSettings
            {
                public int Switch;
                public int[] SwitchLioId;
                public int SwitchDelay;
                public int AmmoId;
                public int UseAmmoAmount;
                public int UseEnergyAmount;
                public int AutoCloseDelay;
                public string SkillLogic;

                public int Frequency;
                public bool AmmoOverridesLogic;
                public bool AmmoOverridesFrequency;
                public bool FrequencyOverridesAmmo;
                public bool FrequencyOverridesLogic;
                public bool LogicOverridesAmmo;
                public bool LogicOverridesFrequency;

                public string SwitchGfxBlobName;
                public string SwitchGfxBlobId;
                public int LightPermutation;
                public int PaletteOffset;
                public int Hue;
                public int Saturation;
                public int Value;
                public int AnimationTime;

                public string SwitchSoundBlobName;
                public string SwitchSoundBlobId;
                public int SwitchSoundSimultaneous;
            }

            public SwitchSettings SwitchData;

            /// <summary>
            /// 
            /// </summary>
            public Switch()
            {
                GeneralData.Type = Types.Switch;

                SwitchData.SwitchLioId = new int[16];
            }

            /// <summary>
            /// Extracts properties for a Switch object from the CSV-formatted line.
            /// </summary>
            /// <param name="values">CSV-formatted line containing properties of a Switch object</param>
            public sealed override void ExtractCsvLine(List<string> values)
            {
                base.ExtractCsvLine(values);

                SwitchData.Switch = CSVReader.GetInt(values[10]);

                for(int i = 0; i < 16; i++)
                    SwitchData.SwitchLioId[i] = CSVReader.GetInt(values[11 + i]);

                SwitchData.SwitchDelay = CSVReader.GetInt(values[27]);
                SwitchData.AmmoId = CSVReader.GetInt(values[28]);
                SwitchData.UseAmmoAmount = CSVReader.GetInt(values[29]);
                SwitchData.UseEnergyAmount = CSVReader.GetInt(values[30]);
                SwitchData.AutoCloseDelay = CSVReader.GetInt(values[31]);
                SwitchData.SkillLogic = CSVReader.GetQuotedString(values[32]);

                SwitchData.Frequency = CSVReader.GetInt(values[33]);
                SwitchData.AmmoOverridesLogic = CSVReader.GetBool(values[34]);
                SwitchData.AmmoOverridesFrequency = CSVReader.GetBool(values[35]);
                SwitchData.FrequencyOverridesAmmo = CSVReader.GetBool(values[36]);
                SwitchData.FrequencyOverridesLogic = CSVReader.GetBool(values[37]);
                SwitchData.LogicOverridesAmmo = CSVReader.GetBool(values[38]);
                SwitchData.LogicOverridesFrequency = CSVReader.GetBool(values[39]);

                SwitchData.SwitchGfxBlobName = CSVReader.GetQuotedString(values[40]);
                SwitchData.SwitchGfxBlobId = CSVReader.GetQuotedString(values[41]);
                SwitchData.LightPermutation = CSVReader.GetInt(values[42]);
                SwitchData.PaletteOffset = CSVReader.GetInt(values[43]);
                SwitchData.Hue = CSVReader.GetInt(values[44]);
                SwitchData.Saturation = CSVReader.GetInt(values[45]);
                SwitchData.Value = CSVReader.GetInt(values[46]);
                SwitchData.AnimationTime = CSVReader.GetInt(values[47]);

                SwitchData.SwitchSoundBlobName = CSVReader.GetQuotedString(values[48]);
                SwitchData.SwitchSoundBlobId = CSVReader.GetQuotedString(values[49]);
                SwitchData.SwitchSoundSimultaneous = CSVReader.GetInt(values[50]);
            }
        }
    }
}
