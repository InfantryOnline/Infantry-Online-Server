using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Infantry_Launcher.Helpers
{
    public class IniFile : Dictionary<string, IniSection>
    {
        /// <summary>
        /// File name of our ini file
        /// </summary>
        public string FileName
        {
            get; private set;
        }

        /// <summary>
        /// Gets a specific value from our ini file, returns empty if not found
        /// </summary>
        public string Get(string element, string section)
        {
            if(HasElement(element) && HasSection(element, section))
            { return this[element][section]; }

            MessageBox.Show(string.Format("Error: Missing '{0} {1}' in your .ini file.", element, section));
            return string.Empty;
        }

        /// <summary>
        /// Main constructor
        /// </summary>
        public IniFile(string fileName)
        {
            FileName = fileName;
        }

        /// <summary>
        /// Adds a line within our .ini file
        /// </summary>
        public string Add(string line)
        {
            if (string.IsNullOrEmpty(line))
                throw new Exception("Ini file must start with a section.");
            if (line.StartsWith("["))
                line = line.TrimStart('[');
            if (line.EndsWith("]"))
                line = line.TrimEnd(']');
            Add(line, new IniSection());
            return line;
        }

        /// <summary>
        /// Loads our .ini file
        /// </summary>
        public bool Load()
        {
            try
            {
                StreamReader streamReader = new StreamReader(FileName);
                string index = "";
                while (streamReader.Peek() != -1)
                {
                    string line = streamReader.ReadLine();
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        index = Add(line);
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
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
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
                StreamWriter streamWriter = new StreamWriter(FileName);
                foreach (string index1 in Keys)
                {
                    streamWriter.WriteLine("[" + index1 + "]");
                    foreach (string index2 in this[index1].Keys)
                        streamWriter.WriteLine(index2 + "=" + this[index1][index2]);
                    streamWriter.WriteLine();
                    streamWriter.Flush();
                }
                streamWriter.Close();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Gets a list of each element within our .ini file
        /// </summary>
        public string[] GetElements()
        {
            string[] strArray = new string[Count];
            byte num = 0;
            foreach (KeyValuePair<string, IniSection> keyValuePair in this)
            {
                strArray[num] = keyValuePair.Key;
                ++num;
            }
            return strArray;
        }

        /// <summary>
        /// Do we have a specific element in the file?
        /// </summary>
        public bool HasElement(string section)
        {
            foreach (KeyValuePair<string, IniSection> keyValuePair in this)
            {
                if (keyValuePair.Key == section)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets all section names under our given element
        /// </summary>
        public string[] GetSections(string element)
        {
            string[] strArray = null;

            if (HasElement(element))
            { strArray = this[element].GetKeys(); }

            return strArray;
        }

        /// <summary>
        /// Do we have a specific section in the given element?
        /// </summary>
        public bool HasSection(string element, string section)
        {
            if (HasElement(element) && this[element].ContainsKey(section))
            { return true; }

            return false;
        }
    }
}