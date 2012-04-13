using System.Collections.Generic;

namespace Assets
{
    public partial class LioInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Flag : LioInfo
        {
            public struct FlagSettings
            {
                public int OddsOfAppearance;
                public int MinPlayerCount;
                public int MaxPlayerCount;

                public int FriendlyOwnedFlagPlayerVisibility;
                public int EnemyOwnedFlagPlayerVisibility;
                public int UnownedFlagPlayerVisibility;
                public int OwnedFlagSpectatorVisibility;
                public int UnownedFlagSpectatorVisibility;

                public int FriendlyFlagLos;
                public int EnemyFlagLos;
                public int UnownedFlagLos;

                public int FlagCarriable;
                public bool IsFlagOwnedWhenCarried;
                public bool IsFlagOwnedWhenDropped;

                public int DropDelay;
                public int DropDelayReset;
                public int DropRadius;

                public int TransferMode;

                public int PeriodicPointsReward;
                public int PeriodicExperienceReward;
                public int PeriodicCashReward;

                public int PickupDelay;

                public int FlagOwnerSpecialRadius;
                public int FlagOwnerSpecialHealRate;
                public int FlagOwnerSpecialEnergyRate;
                public int FlagOwnerSpecialRepairRate;
                public int FlagOwnerSpecialShieldPercent;

                public int FlagGraphicRow;
                public int TurretrGroupId;
                public int FlagRelativeId;
                public string SkillLogic;

                public int[] FlagDroppableTerrains;

                public int NonFlagOwnerSpecialRadius;
                public int NonFlagOwnerSpecialHealRate;
                public int NonFlagOwnerSpecialEnergyRate;
                public int NonFlagOwnerSpecialRepairRate;
                public int NonFlagOwnerSpecialShieldPercent;

                public string FlagGfxBlobName;
                public string FlagGfxBlobId;

                public int LightPermutation;
                public int PaletteOffset;
                public int Hue;
                public int Saturation;
                public int Value;
                public int AnimationTime;

                public string SoundPickupBlobName;
                public string SoundPickupBlobId;
                public int SoundPickupSimultaneous;

                public string SoundDropBlobName;
                public string SoundDropBlobId;
                public int SoundDropSimultaneous;
            }

            public FlagSettings FlagData;

            /// <summary>
            /// 
            /// </summary>
            public Flag()
            {
                GeneralData.Type = Types.Flag;
                FlagData.FlagDroppableTerrains = new int[15];
            }

            /// <summary>
            /// Extracts properties for a Flag object from the CSV-formatted line.
            /// </summary>
            /// <param name="values">CSV-formatted line containing properties of a Flag object</param>
            public sealed override void ExtractCsvLine(List<string> values)
            {
                base.ExtractCsvLine(values);

                FlagData.OddsOfAppearance = CSVReader.GetInt(values[10]);
                FlagData.MinPlayerCount = CSVReader.GetInt(values[11]);
                FlagData.MaxPlayerCount = CSVReader.GetInt(values[12]);
                FlagData.FriendlyOwnedFlagPlayerVisibility = CSVReader.GetInt(values[13]);
                FlagData.EnemyOwnedFlagPlayerVisibility = CSVReader.GetInt(values[14]);
                FlagData.UnownedFlagPlayerVisibility = CSVReader.GetInt(values[15]);
                FlagData.OwnedFlagSpectatorVisibility = CSVReader.GetInt(values[16]);
                FlagData.UnownedFlagSpectatorVisibility = CSVReader.GetInt(values[17]);
                FlagData.FriendlyFlagLos = CSVReader.GetInt(values[18]);
                FlagData.EnemyFlagLos = CSVReader.GetInt(values[19]);
                FlagData.UnownedFlagLos = CSVReader.GetInt(values[20]);

                FlagData.FlagCarriable = CSVReader.GetInt(values[21]);
                FlagData.IsFlagOwnedWhenCarried = CSVReader.GetBool(values[22]);
                FlagData.IsFlagOwnedWhenDropped = CSVReader.GetBool(values[23]);

                FlagData.DropDelay = CSVReader.GetInt(values[24]);
                FlagData.DropDelayReset = CSVReader.GetInt(values[25]);
                FlagData.DropRadius = CSVReader.GetInt(values[26]);

                FlagData.TransferMode = CSVReader.GetInt(values[27]);

                FlagData.PeriodicPointsReward = CSVReader.GetInt(values[28]);
                FlagData.PeriodicExperienceReward = CSVReader.GetInt(values[29]);
                FlagData.PeriodicCashReward = CSVReader.GetInt(values[30]);

                FlagData.PickupDelay = CSVReader.GetInt(values[31]);

                FlagData.FlagOwnerSpecialRadius = CSVReader.GetInt(values[32]);
                FlagData.FlagOwnerSpecialHealRate = CSVReader.GetInt(values[33]);
                FlagData.FlagOwnerSpecialEnergyRate = CSVReader.GetInt(values[34]);
                FlagData.FlagOwnerSpecialRepairRate = CSVReader.GetInt(values[35]);
                FlagData.FlagOwnerSpecialShieldPercent = CSVReader.GetInt(values[36]);

                FlagData.FlagGraphicRow = CSVReader.GetInt(values[37]);
                FlagData.TurretrGroupId = CSVReader.GetInt(values[38]);
                FlagData.FlagRelativeId = CSVReader.GetInt(values[39]);
                FlagData.SkillLogic = CSVReader.GetQuotedString(values[40]);

                for(int i = 0; i < 15; i++)
                    FlagData.FlagDroppableTerrains[i] = CSVReader.GetInt(values[41 + i]);

                FlagData.NonFlagOwnerSpecialRadius = CSVReader.GetInt(values[57]);
                FlagData.NonFlagOwnerSpecialHealRate = CSVReader.GetInt(values[58]);
                FlagData.NonFlagOwnerSpecialEnergyRate = CSVReader.GetInt(values[59]);
                FlagData.NonFlagOwnerSpecialRepairRate = CSVReader.GetInt(values[60]);
                FlagData.NonFlagOwnerSpecialShieldPercent = CSVReader.GetInt(values[61]);

                FlagData.FlagGfxBlobName = CSVReader.GetQuotedString(values[62]);
                FlagData.FlagGfxBlobId = CSVReader.GetQuotedString(values[63]);

                FlagData.LightPermutation = CSVReader.GetInt(values[64]);
                FlagData.PaletteOffset = CSVReader.GetInt(values[65]);
                FlagData.Hue = CSVReader.GetInt(values[66]);
                FlagData.Saturation = CSVReader.GetInt(values[67]);
                FlagData.Value = CSVReader.GetInt(values[68]);
                FlagData.AnimationTime = CSVReader.GetInt(values[69]);

                FlagData.SoundPickupBlobName = CSVReader.GetQuotedString(values[70]);
                FlagData.SoundPickupBlobId = CSVReader.GetQuotedString(values[71]);
                FlagData.SoundPickupSimultaneous = CSVReader.GetInt(values[72]);

                FlagData.SoundDropBlobName = CSVReader.GetQuotedString(values[74]);
                FlagData.SoundDropBlobId = CSVReader.GetQuotedString(values[75]);
                FlagData.SoundDropSimultaneous = CSVReader.GetInt(values[76]);

                //Load the blobs
                BlobsToLoad.Add(FlagData.FlagGfxBlobName);
                BlobsToLoad.Add(FlagData.SoundDropBlobName);
                BlobsToLoad.Add(FlagData.SoundPickupBlobName);
            }
        }
    }
}
