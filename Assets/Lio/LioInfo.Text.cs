using System.Collections.Generic;

namespace Assets
{
    public partial class LioInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class Text : LioInfo
        {
            /// <summary>
            /// 
            /// </summary>
            public struct TextSettings
            {
                public int Color;
                public int Frequency;
                public string Text;
            }

            /// <summary>
            /// 
            /// </summary>
            public TextSettings TextData;

            /// <summary>
            /// 
            /// </summary>
            public Text()
            {
                GeneralData.Type = Types.Text;
            }

            /// <summary>
            /// Extracts properties for a Text object from the CSV-formatted line.
            /// </summary>
            /// <param name="values">CSV-formatted line containing properties of a Text object</param>
            public sealed override void ExtractCsvLine(List<string> values)
            {
                base.ExtractCsvLine(values);

                TextData.Color = CSVReader.GetInt(values[10]);
                TextData.Frequency = CSVReader.GetInt(values[11]);
                TextData.Text = CSVReader.GetQuotedString(values[12]);
            }
        }
    }
}
