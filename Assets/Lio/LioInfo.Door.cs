using System.Collections.Generic;

namespace Assets
{
    public partial class LioInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Door : LioInfo
        {
            /// <summary>
            /// 
            /// </summary>
            public struct DoorSettings
            {
                public int RelativePhysicsTileX;
                public int RelativePhysicsTileY;
                public int PhysicsWidth;
                public int PhysicsHeight;
                public int OpenOdds;
                public int LinkedDoorId;
                public int InitialState;
                public int InverseState;
                public string GfxHorizontalTopBlobName;
                public string GfxHorizontalTopBlobId;
                public int LightPermutation;
                public int PaletteOffset;
                public int Hue;
                public int Saturation;
                public int Value;
                public int AnimationTime;
                public string SoundOpenBlobName;
                public string SoundOpenBlobId;
                public int OpenSimultaneous;
                public string SoundCloseBlobName;
                public string SoundCloseBlobId;
                public int CloseSimultaneous;
            }

            /// <summary>
            /// 
            /// </summary>
            public DoorSettings DoorData;

            /// <summary>
            /// 
            /// </summary>
            public Door()
            {
                GeneralData.Type = Types.Door;
            }

            /// <summary>
            /// Extracts properties for a Door object from the CSV-formatted line.
            /// </summary>
            /// <param name="values">CSV-formatted line containing properties of a Door object</param>
            public sealed override void ExtractCsvLine(List<string> values)
            {
                base.ExtractCsvLine(values);

                DoorData.RelativePhysicsTileX = CSVReader.GetInt(values[10]);
                DoorData.RelativePhysicsTileY = CSVReader.GetInt(values[11]);
                DoorData.PhysicsWidth = CSVReader.GetInt(values[12]);
                DoorData.PhysicsWidth = CSVReader.GetInt(values[13]);
                DoorData.OpenOdds = CSVReader.GetInt(values[14]);
                DoorData.LinkedDoorId = CSVReader.GetInt(values[15]);
                DoorData.InitialState = CSVReader.GetInt(values[16]);
                DoorData.InverseState = CSVReader.GetInt(values[17]);
                DoorData.GfxHorizontalTopBlobName = CSVReader.GetQuotedString(values[18]);
                DoorData.GfxHorizontalTopBlobId = CSVReader.GetQuotedString(values[19]);
                DoorData.LightPermutation = CSVReader.GetInt(values[20]);
                DoorData.PaletteOffset = CSVReader.GetInt(values[21]);
                DoorData.Hue = CSVReader.GetInt(values[22]);
                DoorData.Value = CSVReader.GetInt(values[23]);
                DoorData.Saturation = CSVReader.GetInt(values[24]);
                DoorData.AnimationTime = CSVReader.GetInt(values[25]);
                DoorData.SoundOpenBlobName = CSVReader.GetQuotedString(values[26]);
                DoorData.SoundOpenBlobId = CSVReader.GetQuotedString(values[27]);
                DoorData.OpenSimultaneous = CSVReader.GetInt(values[28]);
                DoorData.SoundCloseBlobName = CSVReader.GetQuotedString(values[30]);
                DoorData.SoundCloseBlobId = CSVReader.GetQuotedString(values[31]);
                DoorData.CloseSimultaneous = CSVReader.GetInt(values[32]);
            }
        }
    }
}
