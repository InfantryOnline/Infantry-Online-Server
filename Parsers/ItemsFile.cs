using System.Collections.Generic;

namespace Parsers
{
    public static class ItemsFile
    {
        private static Item Instantiate(int id)
        {
            switch (id)
            {
                case 1: return new Item.Cash();
                case 4: return new Item.Ammo();
                case 6: return new Item.Projectile();
                case 7: return new Item.VehicleMaker();
                case 8: return new Item.MultiUse();
                case 11: return new Item.Repair();
                case 12: return new Item.Control();
                case 13: return new Item.Utility();
                case 14: return new Item.ItemMaker();
                case 15: return new Item.Upgrade();
                case 16: return new Item.Skill();
                case 17: return new Item.Warp();
                case 18: return new Item.Nested();
            }

            return null;
        }

        public static List<Item> Load(string path)
        {
            return CsvFile<Item>.Load(path, Instantiate, false);
        }
    }
}
