using System.Collections.Generic;

namespace Assets
{
	/// <summary>
	/// Contains data about a particular graphical blo file (*.blo).
	/// </summary>
	public class BloFile : AbstractAsset, IAssetFile<object>
	{
		/// <summary>
		/// Returns Item as the type of asset.
		/// </summary>
		public override AssetTypes Type { get { return AssetTypes.Blo; } }

		/// <summary>
		/// Returns the content of this file as a ItmInfo list. Each ItmInfo is a vehicle type.
		/// </summary>
		public object Data { get { return null; } }

		/// <summary>
		/// Reads in the .blo file and performs crc32 calculations.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="unk1"></param>
		/// <returns></returns>
		internal override void ReadFile(string filename)
		{
			base.ReadFile(filename);
		}
	}
}
