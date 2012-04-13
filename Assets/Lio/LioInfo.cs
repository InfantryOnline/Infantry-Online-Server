using System;
using System.Collections.Generic;
using System.IO;

namespace Assets
{
    /// <summary>
    /// LIO stands for "Level Interactive Objects". These are ingame objects that respond to events. The object classes are:
    /// <list type="unordered">
    ///     <item>Doors</item>
    ///     <item>Flags</item>
    ///     <item>Hides</item>
    ///     <item>Nested</item>
    ///     <item>Parallax</item>
    ///     <item>Portals</item>
    ///     <item>Sounds</item>
    ///     <item>Texts</item>
    ///     <item>Warp Fields</item>
    /// </list>
    /// </summary>
    /// <author>Jovan</author>
    public abstract partial class LioInfo
    {
        /// <summary>
        /// Enumeration of the LIO types.
        /// </summary>
        public enum Types
        {
            /// <summary>
            /// Doors are in-game doors that are activated by specific switch, timing event, or players being in the surrounding area.
            /// </summary>
            Door = 1,

            /// <summary>
            /// A Switch is used to control one or more doors.
            /// </summary>
            Switch = 2,

            /// <summary>
            /// Flags are used to setup objectives that are either static or carried, and have specific properties to the user.
            /// </summary>
            Flag = 3,

            /// <summary>
            /// The Warp Field is where player will emerge after exiting a Portal.
            /// </summary>
            WarpField = 4,

            /// <summary>
            /// Hides are used to place vehicles and items onto the map; they control spawning points and environments.
            /// </summary>
            Hide = 5,

            /// <summary>
            /// Portals are used to teleport the player to Warp Fields.
            /// </summary>
            Portal = 6,

            /// <summary>
            /// Describes ambient noises on the map.
            /// </summary>
            Sound = 7,

            /// <summary>
            /// Used to describe the portion of the map and terrain properties of text, with a pivot at the top-left corner of the map.
            /// </summary>
            Text = 8,

            /// <summary>
            /// Visual effects specifically suited to Cosmic Rift.
            /// </summary>
            Parallax = 9,

            /// <summary>
            /// Allows nesting of one LIO file inside another.
            /// </summary>
            Nested = 10
        }

        /// <summary>
        /// Data shared across all types of LIO objects.
        /// </summary>
        public struct GeneralSettings
        {
            /// <summary>
            /// Type of this object.
            /// </summary>
            public Types Type;

            /// <summary>
            /// Features of this object based on the format.
            /// </summary>
            public string Version;

            /// <summary>
            /// The unique identifier of this object.
            /// </summary>
            public int Id;

            /// <summary>
            /// Name of this object.
            /// </summary>
            public string Name;

            /// <summary>
            /// Pixel offset from top-right (going from left to right).
            /// </summary>
            public short OffsetX;

            /// <summary>
            /// Pixel offset from top-right (going from top to bottom).
            /// </summary>
			public short OffsetY;

            /// <summary>
            /// Horizontal pixel distance defining the region.
            /// </summary>
			public short Width;

            /// <summary>
            /// Vertical pixel distance defining the region.
            /// </summary>
			public short Height;

            /// <summary>
            /// Relative identifier of the object to get x/y coordinates from.
            /// </summary>
            public int RelativeId;

            /// <summary>
            /// Frequency of relative identifier to hunt for in determining this object's location.
            /// </summary>
            public int HuntFrequency;
        }

        /// <summary>
        /// General properties associated with this object.
        /// </summary>
        public GeneralSettings GeneralData;
        public static List<string> BlobsToLoad = new List<string>();

        /// <summary>
        /// Extracts properties for this object from a CSV-formatted line.
        /// </summary>
        /// <param name="values">CSV-formatted line containing properties of this object</param>
        public virtual void ExtractCsvLine(List<string> values)
        {
            GeneralData.Version = CSVReader.GetString(values[1]);
            GeneralData.Id = CSVReader.GetInt(values[2]);
            GeneralData.Name = CSVReader.GetString(values[3]);
            GeneralData.OffsetX = (short)CSVReader.GetInt(values[4]);
			GeneralData.OffsetY = (short)CSVReader.GetInt(values[5]);
			GeneralData.Width = (short)CSVReader.GetInt(values[6]);
			GeneralData.Height = (short)CSVReader.GetInt(values[7]);
            GeneralData.RelativeId = CSVReader.GetInt(values[8]);
            GeneralData.HuntFrequency = CSVReader.GetInt(values[9]);
        }

        public override string ToString()
        {
            return String.Format("{0}({1}) : {2}", GeneralData.Name, GeneralData.Id, GeneralData.Type);
        }

        /// <summary>
        /// Returns a list of LioInfo files extracted from a CSV-formatted .lio file located at filepath.
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static List<LioInfo> Load(string filepath)
        {
            List<List<String>> lines = new List<List<string>>();
            StreamReader stream = new StreamReader(filepath);
            StringReader reader = new StringReader(stream.ReadToEnd());

            for(String line = reader.ReadLine(); line != null; line = reader.ReadLine())
                lines.Add(CSVReader.Parse(line));

            reader.Close();
            stream.Close();
            return CreateLioList(lines);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        protected static List<LioInfo> CreateLioList(List<List<String>> lines)
        {
            List<LioInfo> lios = new List<LioInfo>();

            foreach(List<String> values in lines)
            {
                LioInfo lio;

                switch(CSVReader.GetInt(values[0]))
                {
                    case (int)Types.Door:
                        lio = new Door();
                        break;

                    case (int)Types.Flag:
                        lio = new Flag();
                        break;

                    case (int)Types.Hide:
                        lio = new Hide();
                        break;

                    case (int)Types.Nested:
                        lio = new Nested();
                        break;

                    case (int)Types.Parallax:
                        lio = new Parallax();
                        break;

                    case (int)Types.Portal:
                        lio = new Portal();
                        break;

                    case (int)Types.Sound:
                        lio = new Sound();
                        break;

                    case (int)Types.Switch:
                        lio = new Switch();
                        break;

                    case (int)Types.Text:
                        lio = new Text();
                        break;

                    case (int)Types.WarpField:
                        lio = new WarpField();
                        break;

                    default:
                        throw new InvalidDataException("No valid type provided to LioInfo");
                }

                lio.ExtractCsvLine(values);
                lios.Add(lio);
            }

            return lios;
        }
    }
}
