﻿using System.Collections.Generic;

namespace Assets
{
    public partial class LioInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Hide : LioInfo
        {
            /// <summary>
            /// 
            /// </summary>
            public struct HideSettings
            {
                public int InitialCount;
                public int AttemptDelay;
                public int SucceedDelay;
                public int Probability;
                public int MinPlayers;
                public int MaxPlayers;
                public int MinPlayerDistance;
                public int MaxPlayerDistance;
                public int MaxTypeInArea;
                public int MaxTypeInlevel;

                public int HideId;
                public int HideQuantity;
                public int HideTurretGroup;
                public string HideAnnounce;
                public int RelativeId;

                public int AssignFrequency;
                public int ClumpRadius;
                public int ClumpQuantity;
                public int TurretSwitchedFrequency;
                public int TurretInverseState;
                public int RtsStateNumber;
            }

            /// <summary>
            /// 
            /// </summary>
            public HideSettings HideData;

            /// <summary>
            /// 
            /// </summary>
            public Hide()
            {
                GeneralData.Type = Types.Hide;
            }

            /// <summary>
            /// Extracts properties for a Hide object from the CSV-formatted line.
            /// </summary>
            /// <param name="values">CSV-formatted line containing properties of a Hide object</param>
            public sealed override void ExtractCsvLine(List<string> values)
            {
                base.ExtractCsvLine(values);

                HideData.InitialCount = CSVReader.GetInt(values[10]);
                HideData.AttemptDelay = CSVReader.GetInt(values[11]);
                HideData.SucceedDelay = CSVReader.GetInt(values[12]);
                HideData.Probability = CSVReader.GetInt(values[13]);
                HideData.MinPlayers = CSVReader.GetInt(values[14]);
                HideData.MaxPlayers = CSVReader.GetInt(values[15]);
                HideData.MinPlayerDistance = CSVReader.GetInt(values[16]);
                HideData.MaxPlayerDistance = CSVReader.GetInt(values[17]);
                HideData.MaxTypeInArea = CSVReader.GetInt(values[18]);
                HideData.MaxTypeInlevel = CSVReader.GetInt(values[19]);

                HideData.HideId = CSVReader.GetInt(values[20]);
                HideData.HideQuantity = CSVReader.GetInt(values[21]);
                HideData.HideTurretGroup = CSVReader.GetInt(values[22]);
                HideData.HideAnnounce = CSVReader.GetQuotedString(values[23]);
                HideData.RelativeId = CSVReader.GetInt(values[24]);

                HideData.AssignFrequency = CSVReader.GetInt(values[25]);
                HideData.ClumpRadius = CSVReader.GetInt(values[26]);
                HideData.ClumpQuantity = CSVReader.GetInt(values[27]);
                if (GeneralData.Version != "v25") // LIO versions are kept in the format 'vXX', VEH records however parse this as an int, shrug
                {
                    HideData.TurretSwitchedFrequency = -2;  // default values observed from lio editor
                    HideData.TurretInverseState = 0;
                    HideData.RtsStateNumber = -1;
                }
                else
                {
                    HideData.TurretSwitchedFrequency = CSVReader.GetInt(values[28]);
                    HideData.TurretInverseState = CSVReader.GetInt(values[29]);
                    HideData.RtsStateNumber = CSVReader.GetInt(values[30]);
                }
            }
        }
    }
}