using System.Collections.Generic;

namespace Assets
{
    /// <summary>
    /// Contains data about a particular skills/attributes settings (*.rpg) file.
    /// </summary>
    public class SkillFile : AbstractAsset, IAssetFile<List<SkillInfo>>
    {
        private List<SkillInfo> _data;

        /// <summary>
        /// Returns Skill as the type of asset.
        /// </summary>
        public override AssetTypes Type { get { return AssetTypes.Skill; } }

        /// <summary>
        /// Returns the content of this file as a SkillInfo list. Each SkillInfo is a player/global attribute setting.
        /// </summary>
        public List<SkillInfo> Data { get { return _data; } }

        /// <summary>
        /// Reads in the rpg file and performs crc32 calculations.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="unk1"></param>
        /// <returns></returns>
        internal override void ReadFile(string filename)
        {
            base.ReadFile(filename);

            _data = SkillInfo.Load(filename);
        }
    }
}
