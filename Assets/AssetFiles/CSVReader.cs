using System;
using System.Collections.Generic;

namespace Assets
{
    /// <summary>
    /// Internal CSV parser.
    /// </summary>
    /// <author>Jovan</author>
    /// <author>Toriad</author>
    public static class CSVReader
    {
        /// <summary>
        /// Reads a CSV-formatted line and returns all the comma-delimited tokens. Commas inside of quoted values are preserved.
        /// </summary>
        /// <param name="line">Input line</param>
        /// <returns>List of comma-delimited tokens in the line</returns>
        public static List<String> Parse(string line)
        {
            List<string> values = new List<string>();
            bool inQuotes = false;
            int start = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    if (!inQuotes)
                        inQuotes = true;
                    else
                        inQuotes = false;
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    values.Add((line.Substring(start, (i - start))).Trim());
                    start = i + 1;
                }

            }
            values.Add((line.Substring(start, (line.Length - start))).Trim());
            start = line.Length + 1;

            return values;
        }

        /// <summary>
        /// Returns a 32-bit integer from a string value.
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <returns>The 32-bit integer within the string</returns>
        static public int GetInt(string value)
        {
            return Int32.Parse(value.Trim());
        }

        /// <summary>
        /// Returns a single-precision float from a string value.
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <returns>The single-precision float within the string</returns>
        static public float GetFloat(string value)
        {
            return float.Parse(value.Trim());
        }

        /// <summary>
        /// Returns a pre- and post-trimmed string from a string value.
        /// </summary>
        /// <param name="value">String to trim</param>
        /// <returns>The trimmed string</returns>
        static public string GetString(string value)
        {
            return value.Trim();
        }

        /// <summary>
        /// Returns a quoted string without the double-quotation marks.
        /// </summary>
        /// <param name="value">String to unquote</param>
        /// <returns>The unquoted resulting string</returns>
        static public string GetQuotedString(string value)
        {
            return value.Replace("\"", " ").Trim();
        }

        /// <summary>
        /// Returns a boolean from a string value.
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <returns>The boolean within the string</returns>
        static public bool GetBool(string value)
        {
            return Convert.ToBoolean(Int32.Parse(value.Trim()));
        }
    }
}
