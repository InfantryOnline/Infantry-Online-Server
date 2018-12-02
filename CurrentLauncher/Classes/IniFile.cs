using System;
using System.Collections.Generic;
using System.IO;

namespace Ini
{
    public class IniFile : Dictionary<string, IniSection>
    {
        private string _FileName;

        /// <summary>
        /// File name of our .ini file
        /// </summary>
        public string FileName
        {
            get
            {
                return this._FileName;
            }
        }

        /// <summary>
        /// Main constructor
        /// </summary>
        public IniFile(string file)
        {
            this._FileName = file;
        }

        /// <summary>
        /// Adds a line within our .ini file
        /// </summary>
        public string Add(string line)
        {
            if (line.StartsWith("["))
                line = line.TrimStart('[');
            if (line.EndsWith("]"))
                line = line.TrimEnd(']');
            base.Add(line, new IniSection());
            return line;
        }

        /// <summary>
        /// Loads our .ini file
        /// </summary>
        /// <returns>Returns true if loaded</returns>
        public bool Load()
        {
            if (!this.Exists())
                return true;
            try
            {
                StreamReader streamReader = new StreamReader(this._FileName);
                string index = "";
                while (streamReader.Peek() != -1)
                {
                    string line = streamReader.ReadLine();
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        index = this.Add(line);
                    }
                    else
                    {
                        if (index.Length == 0)
                            throw new Exception("Ini file must start with a section.");
                        this[index].Add(line);
                    }
                }
                streamReader.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Saves our .ini file
        /// </summary>
        /// <returns>Returns true if saved completed</returns>
        public bool Save()
        {
            try
            {
                StreamWriter streamWriter = new StreamWriter(this._FileName);
                foreach (string index1 in this.Keys)
                {
                    streamWriter.WriteLine("[" + index1 + "]");
                    foreach (string index2 in this[index1].Keys)
                        streamWriter.WriteLine(index2 + "=" + this[index1][index2]);
                    streamWriter.WriteLine();
                    ((TextWriter) streamWriter).Flush();
                }
                streamWriter.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Does our .ini file exist?
        /// </summary>
        public bool Exists()
        {
            return File.Exists(this._FileName);
        }

        /// <summary>
        /// Deletes our current .ini file
        /// </summary>
        /// <returns>Returns true if succeeded</returns>
        public bool Delete()
        {
            try
            {
                File.Delete(this._FileName);
                this.Clear();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Moves it to a specific location
        /// </summary>
        /// <returns>Returns true if succeeded</returns>
        public bool Move(string path)
        {
            try
            {
                File.Move(this._FileName, path);
                this._FileName = path;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a list of each section within our .ini file
        /// </summary>
        public string[] GetSections()
        {
            string[] strArray = new string[this.Count];
            byte num = (byte) 0;
            foreach (KeyValuePair<string, IniSection> keyValuePair in (Dictionary<string, IniSection>) this)
            {
                strArray[(int) num] = keyValuePair.Key;
                ++num;
            }
            return strArray;
        }

        /// <summary>
        /// Do we have a specific section in the file?
        /// </summary>
        public bool HasSection(string section)
        {
            foreach (KeyValuePair<string, IniSection> keyValuePair in (Dictionary<string, IniSection>) this)
            {
                if (keyValuePair.Key == section)
                return true;
            }
            return false;
        }
    }
}
