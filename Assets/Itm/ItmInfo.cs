using System;
using System.Collections.Generic;
using System.IO;

namespace Assets
{
    public partial class ItemInfo
    {
        public static List<string> BlobsToLoad = new List<string>();
        public static List<string> FilesToLoad = new List<string>();
        public static List<ItemInfo> Load(string filename)
        {
            List<ItemInfo> itemInfo = new List<ItemInfo>();
            TextReader reader = new StreamReader(filename);
            string line = "";

            while ((line = reader.ReadLine()) != null)
            {
                List<string> values = CSVReader.Parse(line);
                switch (values[0])
                {
                    case "4":
                        Ammo ammo = Ammo.Load(values);
                        BlobsToLoad.Add(ammo.graphic.blobName);
                        itemInfo.Add(ammo);
                        break;
                    case "1":
                        MultiItem multiItem = MultiItem.Load(values);
                        BlobsToLoad.Add(multiItem.graphic.blobName);
                        itemInfo.Add(multiItem);
                        break;
                    case "6":
                        Projectile projectile = Projectile.Load(values);
                        BlobsToLoad.Add(projectile.iconGraphic.blobName);
                        BlobsToLoad.Add(projectile.fireGraphic.blobName);
                        BlobsToLoad.Add(projectile.projectileGraphic.blobName);
                        BlobsToLoad.Add(projectile.shadowGraphic.blobName);
                        BlobsToLoad.Add(projectile.trailGraphic.blobName);
                        BlobsToLoad.Add(projectile.explosionGraphic.blobName);
                        BlobsToLoad.Add(projectile.prefireGraphic.blobName);
                        BlobsToLoad.Add(projectile.firingSound.blobName);
                        BlobsToLoad.Add(projectile.explosionSound.blobName);
                        BlobsToLoad.Add(projectile.bounceSound.blobName);
                        BlobsToLoad.Add(projectile.prefireSound.blobName);
                        itemInfo.Add(projectile);
                        break;
                    case "7":
                        VehicleMaker vehicleMaker = VehicleMaker.Load(values);
                        BlobsToLoad.Add(vehicleMaker.prefireSound.blobName);
                        BlobsToLoad.Add(vehicleMaker.prefireGraphic.blobName);
                        BlobsToLoad.Add(vehicleMaker.iconGraphic.blobName);
                        itemInfo.Add(vehicleMaker);
                        break;
                    case "8":
                        MultiUse multiUse = MultiUse.Load(values);
                        BlobsToLoad.Add(multiUse.prefireGraphic.blobName);
                        BlobsToLoad.Add(multiUse.firingSound.blobName);
                        BlobsToLoad.Add(multiUse.prefireSound.blobName);
                        BlobsToLoad.Add(multiUse.iconGraphic.blobName);
                        itemInfo.Add(multiUse);
                        break;
                    case "11":
                        RepairItem repair = RepairItem.Load(values);
                        BlobsToLoad.Add(repair.iconGraphic.blobName);
                        BlobsToLoad.Add(repair.prefireGraphic.blobName);
                        BlobsToLoad.Add(repair.repairGraphic.blobName);
                        BlobsToLoad.Add(repair.prefireSound.blobName);
                        BlobsToLoad.Add(repair.repairSound.blobName);
                        itemInfo.Add(repair);
                        break;
                    case "15":
                        UpgradeItem upgrade = UpgradeItem.Load(values);
                        BlobsToLoad.Add(upgrade.iconGraphic.blobName);
                        itemInfo.Add(upgrade);
                        break;
                    case "12":
                        ControlItem control = ControlItem.Load(values);
                        BlobsToLoad.Add(control.iconGraphic.blobName);
                        BlobsToLoad.Add(control.prefireGraphic.blobName);
                        BlobsToLoad.Add(control.effectGraphic.blobName);
                        BlobsToLoad.Add(control.prefireSound.blobName);
                        BlobsToLoad.Add(control.firingSound.blobName);
                        itemInfo.Add(control);
                        break;
                    case "13":
                        UtilityItem utility = UtilityItem.Load(values);
                        BlobsToLoad.Add(utility.iconGraphic.blobName);
                        BlobsToLoad.Add(utility.activateSound.blobName);
                        itemInfo.Add(utility);
                        break;
                    case "17":
                        WarpItem warp = WarpItem.Load(values);
                        BlobsToLoad.Add(warp.iconGraphic.blobName);
                        BlobsToLoad.Add(warp.prefireGraphic.blobName);
                        BlobsToLoad.Add(warp.warpGraphic.blobName);
                        BlobsToLoad.Add(warp.prefireSound.blobName);
                        BlobsToLoad.Add(warp.warpSound.blobName);
                        itemInfo.Add(warp);
                        break;
                    case "16":
                        SkillItem skill = SkillItem.Load(values);
                        BlobsToLoad.Add(skill.iconGraphic.blobName);
                        itemInfo.Add(skill);
                        break;
                    case "14":
                        ItemMaker item = ItemMaker.Load(values);
                        BlobsToLoad.Add(item.prefireGraphic.blobName);
                        BlobsToLoad.Add(item.prefireSound.blobName);
                        BlobsToLoad.Add(item.iconGraphic.blobName);
                        itemInfo.Add(item);
                        break;
                    case "18":
                        NestedItem nested = NestedItem.Load(values);
                        FilesToLoad.Add(nested.location);
                        itemInfo.Add(nested);
                        break;
                    case "default":
						//Fuck you, you idiot. use Log.write
                        //Console.WriteLine("If you see this Toriad fucked up");
                        break;

                }
            }

            return itemInfo;
        }
    }
}
