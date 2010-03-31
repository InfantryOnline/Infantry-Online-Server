using System.Collections.Generic;

namespace Assets
{
    /// <summary>
    /// Contains data about a particular interactive-objects file (*.lio).
    /// </summary>
    public sealed class LioFile : AbstractAsset, IAssetFile<List<LioInfo>>
    {
        private List<LioInfo> _data;

        /// <summary>
        /// Returns Lio as the type of asset.
        /// </summary>
        public override AssetTypes Type { get { return AssetTypes.Lio; } }

        /// <summary>
        /// Returns the content of this file as a LioInfo list. Each LioInfo is an interactive object on the map.
        /// </summary>
        public List<LioInfo> Data { get { return _data; } }

        /// <summary>
        /// Reads in the lio file and performs crc32 calculations.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="unk1"></param>
        /// <returns></returns>
        internal override void ReadFile(string filename)
        {
            base.ReadFile(filename);

            _data = LioInfo.Load(filename);
        }
    }
}
