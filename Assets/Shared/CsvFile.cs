using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Assets
{
    /// <summary>
    /// Generic CSV reader.
    /// </summary>
    /// <typeparam name="TType">The base type that items in the CSV represent.</typeparam>
    public static class CsvFile<TType>
        where TType: class, ICsvParseable
    {
        /// <summary>
        /// CSV Line Parser, internal implementation of ICsvParser.
        /// </summary>
        private class LineParser : ICsvParser
        {
            public int Index { get; private set; }
            private List<string> Values;
            public bool AtEnd { get { return this.Index >= this.Values.Count; } }
            
            private static List<string> ParseTokens(string input)
            {
                List<string> values = new List<string>();

                for (int i = 0; i < input.Length; )
                {
                    // Whitespace b-gone!
                    if (char.IsWhiteSpace(input, i) == true)
                    {
                        i++;
                    }
                    // Quoted strings
                    else if (input[i] == '"')
                    {
                        int start = i + 1;
                        int end;
                        
                        // Someone didn't terminate their "
                        end = input.IndexOf('"', start);
                        if (end < 0)
                        {
                            values.Add(input.Substring(start));
                            break;
                        }

                        // Add quoted value
                        values.Add(input.Substring(start, end - start));

                        // Skip the following comma (if there is one)
                        end = input.IndexOf(',', end + 1);
                        if (end >= 0)
                        {
                            i = end + 1;
                        }
                    }
                    else
                    {
                        int end = input.IndexOf(',', i);

                        // Moving on...
                        if (end >= 0)
                        {
                            if (i == end)
                            {
                                values.Add("");
                            }
                            else
                            {
                                values.Add(input.Substring(i, end - i).Trim());
                            }
                            i = end + 1;
                        }
                        // No comma, no more goods baby
                        else
                        {
                            values.Add(input.Substring(i).Trim());
                            break;
                        }
                    }
                }

                return values;
            }

            public LineParser(string input)
            {
                this.Index = 0;
                this.Values = ParseTokens(input);
            }

            public void Skip()
            {
                if (this.AtEnd == true)
                {
                    throw new InvalidOperationException("out of data");
                }

                this.Index++;
            }

            public void Skip(int count)
            {
                if (this.AtEnd == true || this.Index + count > this.Values.Count)
                {
                    throw new InvalidOperationException("out of data");
                }

                this.Index += count;
            }

            public bool GetBool()
            {
                if (this.AtEnd == true)
                {
                    throw new InvalidOperationException("out of data");
                }

                int dummy = 0;
                if (int.TryParse(this.GetString(), out dummy) == false)
                {
                    throw new InvalidOperationException("failed to parse bool");
                }

                return Convert.ToBoolean(dummy);
            }

            public sbyte GetSByte()
            {
                if (this.AtEnd == true)
                {
                    throw new InvalidOperationException("out of data");
                }

                sbyte value;

                if (sbyte.TryParse(this.GetString(), out value) == false)
                {
                    throw new InvalidOperationException("failed to parse byte");
                }

                return value;
            }

            public short GetShort()
            {
                if (this.AtEnd == true)
                {
                    throw new InvalidOperationException("out of data");
                }

                string s = this.GetString();
                short value;

                if (s.Length >= 2 && (s[1] == 'x' || s[1] == 'X'))
                {
                    if (short.TryParse(s.Substring(2), NumberStyles.AllowHexSpecifier, null, out value) == false)
                    {
                        int dummy;
                        if (int.TryParse(s.Substring(2), NumberStyles.AllowHexSpecifier, null, out dummy) == false)
                        {
                            throw new InvalidOperationException("failed to parse short as hex");
                        }
                        value = (short)dummy;
                        // TODO: warn about a clamped value
                    }
                }
                else
                {
                    if (short.TryParse(s, out value) == false)
                    {
                        int dummy;
                        if (int.TryParse(s, out dummy) == false)
                        {
                            throw new InvalidOperationException("failed to parse short");
                        }
                        value = (short)dummy;
                        // TODO: warn about a clamped value
                    }
                }

                return value;
            }

            public int GetInt()
            {
                if (this.AtEnd == true)
                {
                    throw new InvalidOperationException("out of data");
                }

                string s = this.GetString();
                int value;

                if (s.Length >= 2 && (s[1] == 'x' || s[1] == 'X'))
                {
                    if (int.TryParse(s.Substring(2), NumberStyles.AllowHexSpecifier, null, out value) == false)
                    {
                        throw new InvalidOperationException("failed to parse integer as hex");
                    }
                }
                else
                {
                    if (int.TryParse(s, out value) == false)
                    {
                        throw new InvalidOperationException("failed to parse integer");
                    }
                }

                return value;
            }

            public int GetInt(params char[] trim)
            {
                if (this.AtEnd == true)
                {
                    throw new InvalidOperationException("out of data");
                }

                int value;

                if (int.TryParse(this.GetString().Trim(trim), out value) == false)
                {
                    throw new InvalidOperationException("failed to parse integer");
                }

                return value;
            }

            public float GetFloat()
            {
                if (this.AtEnd == true)
                {
                    throw new InvalidOperationException("out of data");
                }

                float value;

                if (float.TryParse(this.GetString(), out value) == false)
                {
                    throw new InvalidOperationException("failed to parse float");
                }

                return value;
            }

            public string GetString()
            {
                if (this.AtEnd == true)
                {
                    throw new InvalidOperationException("out of data");
                }

                return this.Values[this.Index++];
            }

            public string GetQuotedString()
            {
                return GetString().Replace("\"", " ").Trim();
            }

            public void GetInstance<TInstanceType>(ref TInstanceType value)
                where TInstanceType : class, ICsvParseable
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                else if (this.AtEnd == true)
                {
                    throw new InvalidOperationException("out of data");
                }

                value.Parse(this);
            }

            public TInstanceType GetInstance<TInstanceType>()
                where TInstanceType : class, ICsvParseable, new()
            {
                if (this.AtEnd == true)
                {
                    throw new InvalidOperationException("out of data");
                }

                TInstanceType instance = new TInstanceType();
                this.GetInstance(ref instance);
                return instance;
            }

            public short[] GetShorts(int count)
            {
                if (this.AtEnd == true)
                {
                    throw new InvalidOperationException("out of data");
                }

                short[] values = new short[count];
                for (int i = 0; i < count; i++)
                {
                    values[i] = this.GetShort();
                }
                return values;
            }

            public int[] GetInts(int count)
            {
                if (this.AtEnd == true)
                {
                    throw new InvalidOperationException("out of data");
                }

                int[] values = new int[count];
                for (int i = 0; i < count; i++)
                {
                    values[i] = this.GetInt();
                }
                return values;
            }

            public TInstanceType[] GetInstances<TInstanceType>(int count)
                where TInstanceType : class, ICsvParseable, new()
            {
                if (this.AtEnd == true)
                {
                    throw new InvalidOperationException("out of data");
                }

                TInstanceType[] instances = new TInstanceType[count];
                for (int i = 0; i < count; i++)
                {
                    instances[i] = new TInstanceType();
                    this.GetInstance(ref instances[i]);
                }
                return instances;
            }
        }

        /// <summary>
        /// Read all lines in a text file.
        /// </summary>
        /// <param name="path">Path of the file to consume</param>
        /// <returns>Consumed lines</returns>
        private static List<string> Consume(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            TextReader reader = new StreamReader(path);

            List<string> lines = new List<string>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }

            reader.Close();
            return lines;
        }

        /// <summary>
        /// Load a single-type CSV file.
        /// </summary>
        /// <param name="path">Path of CSV to load</param>
        /// <param name="instantiate">Instantiator of instances</param>
        /// <returns>A list of parsed instances</returns>
        public static List<TType> Load(string path, Func<TType> instantiate)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            List<TType> instances = new List<TType>();

            foreach (string line in Consume(path))
            {
                LineParser parser = new LineParser(line);

                TType instance = instantiate();

                if (instance == null)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            "did not get instance of {0}",
                            typeof(TType)));
                }

                parser.GetInstance(ref instance);

                if (parser.AtEnd == false)
                {
                    throw new InvalidOperationException("did not consume all data");
                }

                instances.Add(instance);
            }

            return instances;
        }

        /// <summary>
        /// Load a multi-type CSV file.
        /// </summary>
        /// <param name="path">Path of CSV to load</param>
        /// <param name="instantiate">Instantiator of instances</param>
        /// <returns>A list of parsed instances</returns>
        public static List<TType> Load(string path, Func<int, TType> instantiate)
        {
            return Load(path, instantiate, true);
        }

        /// <summary>
        /// Load a multi-type CSV file.
        /// </summary>
        /// <param name="path">Path of CSV to load</param>
        /// <param name="instantiate">Instantiator of instances</param>
        /// <param name="ignoreUnknownTypes">Should an exception be thrown if the instantiator returns null?</param>
        /// <returns>A list of parsed instances</returns>
        public static List<TType> Load(string path, Func<int, TType> instantiate, bool ignoreUnknownTypes)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            List<TType> instances = new List<TType>();

            foreach (string line in Consume(path))
            {
                LineParser parser = new LineParser(line);

                int typeId = parser.GetInt();
                TType instance = instantiate(typeId);

                if (instance == null)
                {
                    if (ignoreUnknownTypes == false)
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "unhandled type {0} for {1}",
                                typeId,
                                typeof(TType)));
                    }

                    continue;
                }

                parser.GetInstance(ref instance);

                if (parser.AtEnd == false)
                {
                    throw new InvalidOperationException("did not consume all data");
                }

                instances.Add(instance);
            }

            return instances;
        }
    }
}
