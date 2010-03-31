using System.Collections.Generic;

namespace Assets
{
    public partial class LioInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Nested : LioInfo
        {
            /// <summary>
            /// 
            /// </summary>
            public struct NestedSetting
            {
                public string NestedLioFileName;
            }

            /// <summary>
            /// 
            /// </summary>
            public NestedSetting NestedData;

            /// <summary>
            /// 
            /// </summary>
            public Nested()
            {
                GeneralData.Type = Types.Nested;
            }

            /// <summary>
            /// Extracts properties for a Nested object from the CSV-formatted line.
            /// </summary>
            /// <param name="values">CSV-formatted line containing properties of a Nested object</param>
            public sealed override void ExtractCsvLine(List<string> values)
            {
                GeneralData.Version = CSVReader.GetString(values[1]);
                GeneralData.Id = CSVReader.GetInt(values[2]);
                GeneralData.Name = CSVReader.GetString(values[3]);

                NestedData.NestedLioFileName = CSVReader.GetQuotedString(values[4]);
            }
        }
    }
}
