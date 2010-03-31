using System.Collections.Generic;

namespace Assets
{
    /// <summary>
    /// Contains data about a particular item file (*.itm).
    /// </summary>
    public class ItemFile : AbstractAsset, IAssetFile<List<ItemInfo>>
    {
        private List<ItemInfo> _data;

        /// <summary>
        /// Returns Item as the type of asset.
        /// </summary>
        public override AssetTypes Type { get { return AssetTypes.Item; } }

        /// <summary>
        /// Returns the content of this file as a ItmInfo list. Each ItmInfo is a vehicle type.
        /// </summary>
        public List<ItemInfo> Data { get { return _data; } }

        /// <summary>
        /// Reads in the .itm file and performs crc32 calculations.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="unk1"></param>
        /// <returns></returns>
        internal override void ReadFile(string filename)
        {
            base.ReadFile(filename);

            _data = ItemInfo.Load(filename);
        }
    }
}
