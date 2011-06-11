using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets
{
	/// <summary>
	/// Contains data about a particular graphical blo file (*.blo).
	/// </summary>
	public class BloFile : AbstractAsset, IAssetFile<List<BloFile.FileEntry>>
	{
        /// <summary>
        /// A file located in the blo, 
        /// </summary>
        public class FileEntry
        {
            public FileEntry(string filename, byte[] data )
            {
                Filename = filename;
                Data = data;
            }

            public string Filename;
            public byte[] Data;
        }

		/// <summary>
		/// Returns Blo as the type of asset.
		/// </summary>
		public override AssetTypes Type { get { return AssetTypes.Blo; } }

	    /// <summary>
	    /// Returns all the files and their data located inside of this blob, or null if ReadBlobEntries has not been called.
	    /// </summary>
        public List<FileEntry> Data { get; private set; }

        /// <summary>
        /// Opens the Blob and parses all the files within it. Upon completion, the Data property has a list of the files and
        /// their data payloads.
        /// </summary>
        public void ReadBlobEntries()
        {
            Stream stream = new FileStream(Filename, FileMode.Open, FileAccess.Read);
            Data = new List<FileEntry>();

            var header = new {Version = stream.ReadInt(), NumOfFiles = stream.ReadInt()};
            var fdList = (new[] {new {Filename = "", Offset = 0, Length = 0}}).ToList();

            int filenameLength = header.Version == 1 ? 14 : 32;
            fdList.Clear();

            for (int i = 0; i < header.NumOfFiles; i++)
            {
                string filename = Encoding.ASCII.GetString(stream.ReadByteArray(filenameLength)).Trim('\0');
                fdList.Add(new {Filename = filename, Offset = stream.ReadInt(), Length = stream.ReadInt()});
            }

            foreach(var fd in fdList)
            {
                stream.Seek(fd.Offset, SeekOrigin.Begin);
                Data.Add(new FileEntry(fd.Filename, stream.ReadByteArray(fd.Length)));
            }

            stream.Close();
        }
	}
}
