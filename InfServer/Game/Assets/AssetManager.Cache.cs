using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Assets;

using InfServer.Protocol;

using Ionic.Zlib;

namespace InfServer.Game
{
	/// <summary>
	/// Keeps track of files that a zone uses and their checksums
	/// </summary>
	public partial class AssetManager
	{
		/// <summary>
		/// Keeps compressed files in memory for easy retrieval
		/// </summary>
		public class Cache
		{	// Member variables
			///////////////////////////////////////////////////
			private AssetManager _manager;
			private Dictionary<string, CachedAsset> _cache;

			public class CachedAsset
			{
				public string filepath;			//The path to our asset file

				public bool bCached;			//Has the file been cached yet?
				public uint checksum;			//The checksum of the file at the time of loading

				public byte[] data;				//File data
				public int uncompressedSize;	//The uncompressed file size
			}


			///////////////////////////////////////////////////
			// Member Functions
			///////////////////////////////////////////////////
			/// <summary>		
			/// Resolves all Lio information
			/// </summary>
			public Cache(AssetManager manager)
			{
				_manager = manager;
				_cache = new Dictionary<string, CachedAsset>();
			}

			/// <summary>
			/// Compresses each asset in the list for later use
			/// </summary>		
			public void prepareCache(List<AssetInfo> assets)
			{	//For each asset..
				foreach (AssetInfo asset in assets)
				{	//Insert some details
					CachedAsset cache = new CachedAsset();

					cache.filepath = asset.filepath;
					cache.bCached = false;
					cache.checksum = asset.checksum;

					_cache.Add(Path.GetFileName(asset.filepath), cache);
				}
			}

			/// <summary>
			/// Reads in a file in portions
			/// </summary>	
			private static void copyStream(Stream input, Stream output)
			{	//Read in 2000h bytes at a time
				byte[] buffer = new byte[492];
				int len;

				while ((len = input.Read(buffer, 0, 492)) > 0)
					output.Write(buffer, 0, len);
			}

			/// <summary>
			/// Compresses each asset in the list for later use
			/// </summary>		
			private CachedAsset cacheFile(CachedAsset asset)
			{	//Does it exist?
				if (!File.Exists(asset.filepath))
				{
					Log.write(TLog.Error, "Unable to cache asset '{0}', file was not found.", asset.filepath);
					return null;
				}

				//Quickly compare the checksum
				if (asset.checksum != Assets.CRC32.fileChecksum(asset.filepath))
				{
					Log.write(TLog.Error, "Checksum mismatch on asset '{0}' while attempting to cache.", asset.filepath);
					return null;
				}

				//Load the file into memory
				FileStream fs = new FileStream(asset.filepath, FileMode.Open);

				//Compress it with zlib
				MemoryStream buf = new MemoryStream();
				ZlibStream zlib = new ZlibStream(buf, CompressionMode.Compress);

				copyStream(fs, zlib);
				zlib.Close();

				//Store our cached asset data
				asset.data = buf.ToArray();
				asset.uncompressedSize = (int)fs.Length;
				
				fs.Close();
				buf.Close();

				File.WriteAllBytes("cache.dat", asset.data);
				return asset;
			}

			/// <summary>
			/// Used to access assets of a specified filename
			/// </summary>	
			public CachedAsset this[string filename]
			{
				get
				{	//Obtain the cache
					CachedAsset cache;

					if (!_cache.TryGetValue(filename, out cache))
						return null;

					//Is it currently cached?
					if (!cache.bCached)
						//Quickly cache it
						return cacheFile(cache);
					else
						return cache;
				}
			}
		}
	}
}
