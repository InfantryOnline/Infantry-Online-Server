using System.Collections.Generic;

namespace Assets
{
    /// <summary>
    /// Contains data about a particular vehicle file (*.veh).
    /// </summary>
    public sealed class VehicleFile : AbstractAsset, IAssetFile<List<VehInfo>>
    {
        private List<VehInfo> _data;

        /// <summary>
        /// Returns Vehicle as the type of asset.
        /// </summary>
        public override AssetTypes Type { get { return AssetTypes.Vehicle; } }

        /// <summary>
        /// Returns the content of this file as a VehInfo list. Each VehInfo is a vehicle type.
        /// </summary>
        public List<VehInfo> Data { get { return _data; } }

        /// <summary>
        /// Reads in the veh file and performs crc32 calculations.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="unk1"></param>
        /// <returns></returns>
        internal override void ReadFile(string filename)
        {
            base.ReadFile(filename);

            _data = VehInfo.Load(filename);
        }
    }
}
