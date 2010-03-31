using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets
{
    /// <summary>
    /// Contains data about a particular config file (*.cfg);
    /// </summary>
    public class ConfigFile : AbstractAsset, IAssetFile<CfgInfo>
    {
        private CfgInfo _data;

        /// <summary>
        /// Returns Vehicle as the type of asset.
        /// </summary>
        public override AssetTypes Type { get { return AssetTypes.Vehicle; } }

        /// <summary>
        /// Returns the content of this file as a VehInfo list. Each VehInfo is a vehicle type.
        /// </summary>
        public CfgInfo Data { get { return _data; } }

        /// <summary>
        /// Reads in the veh file and performs crc32 calculations.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="unk1"></param>
        /// <returns></returns>
        internal override void ReadFile(string filename)
        {
            base.ReadFile(filename);

            _data = CfgInfo.Load(filename);
        }
    }
}
