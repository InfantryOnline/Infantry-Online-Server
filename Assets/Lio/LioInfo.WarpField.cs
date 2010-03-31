using System.Collections.Generic;

namespace Assets
{
    public partial class LioInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class WarpField : LioInfo
        {
			/// <summary>
			/// 
			/// </summary>
			public enum WarpMode
			{
				Anybody,
				Unassigned,
				SpecificTeam,
				TeamMultiple,
				SessionMultiple,
			}

            /// <summary>
            /// 
            /// </summary>
            public struct WarpFieldSettings
            {
                public int MinPlayerCount;
                public int MaxPlayerCount;
                public int MinPlayersInArea;
                public int MaxPlayersInArea;
                public int WarpGroup;
				public WarpMode WarpMode;
                public int WarpModeParameter;
                public string SkillLogic;
            }

            /// <summary>
            /// 
            /// </summary>
            public WarpFieldSettings WarpFieldData;

            /// <summary>
            /// 
            /// </summary>
            public WarpField()
            {
                GeneralData.Type = Types.WarpField;
            }

            /// <summary>
            /// Extracts properties for a WarpField object from the CSV-formatted line.
            /// </summary>
            /// <param name="values">CSV-formatted line containing properties of a WarpField object</param>
            public sealed override void ExtractCsvLine(List<string> values)
            {
                base.ExtractCsvLine(values);

                WarpFieldData.MinPlayerCount = CSVReader.GetInt(values[10]);
                WarpFieldData.MaxPlayerCount = CSVReader.GetInt(values[11]);
                WarpFieldData.MinPlayersInArea = CSVReader.GetInt(values[12]);
                WarpFieldData.MaxPlayersInArea = CSVReader.GetInt(values[13]);
                WarpFieldData.WarpGroup = CSVReader.GetInt(values[14]);
                WarpFieldData.WarpMode = (WarpMode)CSVReader.GetInt(values[15]);
                WarpFieldData.WarpModeParameter = CSVReader.GetInt(values[16]);
                WarpFieldData.SkillLogic = CSVReader.GetQuotedString(values[17]);
            }
        }
    }
}
