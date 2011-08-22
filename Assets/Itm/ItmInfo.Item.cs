using System.Collections.Generic;

namespace Assets
{
    public partial class ItemInfo
    {

        public enum PickupMode
        {
            Manual = 0,
            ManualAutoAll,
            ManualAutoNeed,
            AutoAll,
            AutoNeed,
            AutoHaveNone
        }

		public enum ItemType
		{
			Multi = 1,
			Ammo = 4,
			Projectile = 6,
			VehicleMaker = 7,
			MultiUse = 8,
			Repair = 11,
			Control = 12,
			Utility = 13,
			ItemMaker = 14,
			Upgrade = 15,
			Skill = 16,
			Warp = 17,
			Nested = 18,
		}

        public const int AmmoType = 4;

		public ItemType itemType;
        public int version;
        public int id;
        public string name;
        public string category;
        public string skillLogic;
        public string description;
        public float weight;
        public int buyPrice;
        public int probability;
        public bool droppable;
        public int keyPreference;
        public int recommended;
        public int maxAllowed;
        public PickupMode pickupMode;
        public int sellPrice;
        public int expireTimer;
        public int radarColor;
        public int prizeBountyPoints;
        public int relativeID;
        public int heldCategoryType;
        public int pruneDropPercent;
        public int pruneOdds;

        public static void LoadGeneralSettings1(ItemInfo item, List<string> values)
        {
            item.itemType = (ItemType)CSVReader.GetInt(values[0]);
            item.version = CSVReader.GetInt(values[1].Trim('v'));
            item.id = CSVReader.GetInt(values[2]);
			item.name = CSVReader.GetQuotedString(values[3]);
			item.category = CSVReader.GetQuotedString(values[4]);
            item.skillLogic = CSVReader.GetQuotedString(values[5]);
			item.description = CSVReader.GetQuotedString(values[6]);
            item.weight = CSVReader.GetFloat(values[7]);
            item.buyPrice = CSVReader.GetInt(values[8]);
            item.probability = CSVReader.GetInt(values[9]);
            item.droppable = CSVReader.GetBool(values[10]);
            item.keyPreference = CSVReader.GetInt(values[11]);
            item.recommended = CSVReader.GetInt(values[12]);
            item.maxAllowed = CSVReader.GetInt(values[13]);
            item.pickupMode = (PickupMode)CSVReader.GetInt(values[14]);
            item.sellPrice = CSVReader.GetInt(values[15]);
            item.expireTimer = CSVReader.GetInt(values[16]);
            item.radarColor = CSVReader.GetInt(values[17]);
            item.prizeBountyPoints = CSVReader.GetInt(values[18]);
            item.relativeID = CSVReader.GetInt(values[19]);
            item.heldCategoryType = CSVReader.GetInt(values[20]);
            item.pruneDropPercent = CSVReader.GetInt(values[21]);
            item.pruneOdds = CSVReader.GetInt(values[22]);
        }

		public virtual bool getAmmoType(out int ammoID, out int ammoCount)
		{
			ammoID = 0;
			ammoCount = 0;
			return false;	
		}

		public virtual bool getAmmoType(out int ammoID, out int ammoCount, out int ammoCapacity)
		{
			ammoID = 0;
			ammoCount = 0;
			ammoCapacity = 0;
			return false;
		}

		public virtual bool getRouteRange(out int range)
		{
			range = 0;
			return false;
		}
    }
}

