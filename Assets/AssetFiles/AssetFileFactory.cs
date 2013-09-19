using System;
using System.Collections.Generic;
using System.IO;

namespace Assets
{
    /// <summary>
    /// Responsible for creating instances of IAssetFile objects, which themselves contain data about their respective file types (lio, veh, ...).
    /// Any file that needs to be loaded must extend AssetFileReader and IAssetFile.
    /// </summary>
    public static class AssetFileFactory
    {
		/// <summary>
		/// Contains a list of missing files
		/// </summary>
		static public List<string> _missingFiles = new List<string>();

		/// <summary>
		/// Were any files marked as incomplete?
		/// </summary>
		static public bool IsIncomplete
		{
			get
			{
				return _missingFiles.Count > 0;
			}
		}

        /// <summary>
        /// Loads an asset object from a file.
        /// </summary>
        /// <typeparam name="TAsset">Asset type</typeparam>
        /// <param name="filename">File to read</param>
        /// <param name="unk1">Unknown value</param>
        /// <returns>The loaded asset object.</returns>
        public static TAsset CreateFromFile<TAsset>(string filename) where TAsset : AbstractAsset, new()
        {	//Sanity checks
            if (filename == null)
                throw new ArgumentNullException("Null filename.");

			//Attempt to find the file in the directory structure
			string filePath = findAssetFile(filename, "assets\\");
			if (filePath == null)
			{	//It's missing!
				_missingFiles.Add(filename);
				return null;
			}

            TAsset t = new TAsset();
			t.ReadFile(filePath);
            return t;
        }

        /// <summary>
        /// Loads an asset object from a file.
        /// </summary>
        /// <typeparam name="TAsset">Asset type</typeparam>
        /// <param name="filename">File to read</param>
        /// <param name="unk1">Unknown value</param>
        /// <returns>The loaded asset object.</returns>
        public static TAsset LoadBlobFromFile<TAsset>(string filename) where TAsset : AbstractAsset, new()
        {	//Sanity checks
            if (filename == null)
                throw new ArgumentNullException("Null filename.");

            //Attempt to find the file in the directory structure
            string filePath = findAssetFile(filename, "..\\Blobs\\");
            if (filePath == null)
            {	//It's missing!
                _missingFiles.Add(filename);
                return null;
            }

            TAsset t = new TAsset();
            t.ReadFile(filePath);
            return t;
        }

        public static TAsset CreateFromGlobalFile<TAsset>(string filename) where TAsset : AbstractAsset, new()
        {	//Sanity checks
            if (filename == null)
                throw new ArgumentNullException("Null filename.");

            //Attempt to find the file in the directory structure
            string filePath = findAssetFile(filename, @"../Global/");
            if (filePath == null)
            {	//It's missing!
                _missingFiles.Add(filename);
                return null;
            }

            TAsset t = new TAsset();
            t.ReadFile(filePath);
            return t;
        }

		/// <summary>
        /// Locates an asset file in the server's directory tree
        /// </summary>
		public static string findAssetFile(string filename, string path)
		{
            //Does the directory we're looking for exist?
            if (!Directory.Exists(path))
                return null;

            //Does the file exist here?
			string filePath = Path.Combine(path, filename);

			if (File.Exists(filePath))
				return filePath;

			//Otherwise, search the inner directories
			foreach (string dir in Directory.GetDirectories(path))
			{
				filePath = findAssetFile(filename, dir);
				if (filePath != null)
					return filePath;
			}

			return null;
		}
    }
}
