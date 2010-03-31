using System;
using System.IO;

namespace Assets
{
    /// <summary>
    /// Enumeration of the global asset types (veh, lio, itm, ...).
    /// </summary>
    public enum AssetTypes
    {
        /// <summary>
        /// This asset is an item.
        /// </summary>
        Item,

        /// <summary>
        /// This asset is an interactive object on the map.
        /// </summary>
        Lio,

        /// <summary>
        /// This asset contains information about player upgrades, classes, and other attributes.
        /// </summary>
        Skill,

        /// <summary>
        /// This asset is a player or computer controlled vehicle/infantry.
        /// </summary>
        Vehicle,

        /// <summary>
        /// This asset is a game level.
        /// </summary>
        Level,

		/// <summary>
		/// This asset is a miscellaneous game asset.
		/// </summary>
		Misc,

		/// <summary>
		/// This asset is a graphical asset.
		/// </summary>
		Blo,
    }

    public abstract class AbstractAsset
    {
        /// <summary>
        /// Opens an asset file for reading.
        /// </summary>
        /// <param name="filename">File to open</param>
        /// <param name="unk1">Unknown boolean value</param>
        internal virtual void ReadFile(string filename)
        {
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }

			Filename = filename;
            Checksum = CRC32.fileChecksum(filename);
        }

        /// <summary>
        /// Returns the filename of this asset.
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Returns the calculated CRC32 value of this asset.
        /// </summary>
        public uint Checksum { get; private set; }

        /// <summary>
        /// Returns the type of this asset file.
        /// </summary>
        public abstract AssetTypes Type { get; }
    }
}
