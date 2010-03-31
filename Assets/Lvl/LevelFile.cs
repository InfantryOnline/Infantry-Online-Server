namespace Assets
{
    /// <summary>
    /// Contains data about a particular level file (*.lvl).
    /// </summary>
    public class LevelFile : AbstractAsset, IAssetFile<LvlInfo>
    {
        private LvlInfo _data;

        /// <summary>
        /// Returns Level as the type of asset.
        /// </summary>
        public override AssetTypes Type { get { return AssetTypes.Level; } }

        /// <summary>
        /// Returns the content of this file as a strongly typed Level object.
        /// </summary>
        public LvlInfo Data { get { return _data; } }

        /// <summary>
        /// Reads in the .lvl file and performs crc32 calculations.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="unk1"></param>
        /// <returns></returns>
        internal override void ReadFile(string filename)
        {
            base.ReadFile(filename);

            _data = LvlInfo.Load(filename);
        }
    }
}
