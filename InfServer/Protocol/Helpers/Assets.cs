using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Assets;

namespace InfServer.Protocol
{
	/// <summary>
	/// Helper functions to deal with assets
	/// </summary>
	public partial class Helpers
	{
		public static int getMaxBlastRadius(ItemInfo.Projectile wep)
		{
			int blastRadius = 0;

			if (wep.kineticDamageRadius > blastRadius) blastRadius = wep.kineticDamageRadius;
			if (wep.explosiveDamageRadius > blastRadius) blastRadius = wep.explosiveDamageRadius;
			if (wep.electronicDamageRadius > blastRadius) blastRadius = wep.electronicDamageRadius;
			if (wep.psionicDamageRadius > blastRadius) blastRadius = wep.psionicDamageRadius;
			if (wep.bypassDamageRadius > blastRadius) blastRadius = wep.bypassDamageRadius;
			if (wep.energyDamageRadius > blastRadius) blastRadius = wep.energyDamageRadius;

			return blastRadius;
		}
	}

	/// <summary>
	/// Asset info used to sync with client
	/// </summary>
	public class AssetInfo
	{
		public AssetInfo(string fn, uint cs)
		{
			this.filepath = fn;
			this.checksum = cs;
		}

		//Name and checksum of the file
		public string filepath;
		public uint checksum;
	}
}
