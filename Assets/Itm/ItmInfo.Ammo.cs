using System.Collections.Generic;

namespace Assets
{
    public partial class ItemInfo
    {
        public class Ammo : ItemInfo
        {
            public Graphics graphic;


            public static Ammo Load(List<string> values)
            {
                Ammo ammo = new Ammo();
                ammo.graphic = new Graphics(ref values, 23);
                ItemInfo.LoadGeneralSettings1((ItemInfo)ammo, values);

                return ammo;
            }

        }
    }
}
