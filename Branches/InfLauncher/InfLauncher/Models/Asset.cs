using System;
using InfLauncher.Protocol;

namespace InfLauncher.Models
{
    /// <summary>
    /// An asset is a file downloaded from the asset repository.
    /// </summary>
    public class Asset
    {
        /// <summary>
        /// The descriptor associated with this asset.
        /// </summary>
        public AssetDownloader.AssetDescriptor Descriptor { get; private set; }

        /// <summary>
        /// The name of this asset, excluding directories and extensions.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// The binary data associated with this file. May be compressed.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Creates a new Asset object given it's filename and the data.
        /// </summary>
        /// <param name="fileName">The filename, excluding any paths</param>
        /// <param name="data">The byte array of data</param>
        public Asset(string fileName, byte[] data, AssetDownloader.AssetDescriptor descriptor)
        {
            if(fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }
            if(descriptor == null)
            {
                throw new ArgumentNullException("descriptor");
            }
            if(data == null)
            {
                throw new ArgumentNullException("data");
            }

            FileName = fileName;
            Data = data;
            Descriptor = descriptor;
        }
    }
}
