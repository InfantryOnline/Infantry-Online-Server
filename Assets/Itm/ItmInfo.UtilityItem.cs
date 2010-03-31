using System.Collections.Generic;

namespace Assets
{
    public partial class ItemInfo
    {
        public class UtilityItem : ItemInfo
        {
            public Graphics iconGraphic;
            public Sound activateSound;
            public int maxSlotted;
            public int maxSlottedCategoryCode;
            public int extraCapacity;
            public int rechargeRate;
            public int stealthDistance;
            public int cloakDistance;
            public int antiStealthDistance;
            public int antiCloakDistance;
            public int antiWarpDistance;
            public int stealthDistanceTime;
            public int cloakDistanceTime;
            public int vehicleTopSpeed;
            public int vehicleHyperTopSpeed;
            public int vehicleThrust;
            public int vehicleRotate;
            public int soccerThrowTimer;
            public StartMode startMode;
            public enum StartMode
            {
                Off,
                On,
                AlwaysOn,
            }

            public int kineticIgnore;
            public int kineticPercent;
            public int explosiveIgnore;
            public int explosivePercent;
            public int electronicIgnore;
            public int electronicPercent;
            public int psionicIgnore;
            public int psionicPercent;
            public int bypassIgnore;
            public int bypassPercent;
            public int energyIgnore;
            public int energyPercent;

            public int losDistance;
            public int losAngle;
            public int losXray;
            public int losDistancePercent;
            public int losAnglePercent;
            public int combatAwareness;
            public int radarPrizeDistance;
            public int normalWeight;
            public int stopWeight;
            public int strafeThrust;
            public int forwardThrust;
            public int reverseThrust;
            public int soccerCatchRadius;

            public static UtilityItem Load(List<string> values)
            {
                UtilityItem item = new UtilityItem();
                item.iconGraphic = new Graphics(ref values, 23);
                item.activateSound = new Sound(ref values, 48);
                ItemInfo.LoadGeneralSettings1((ItemInfo)item, values);
                item.maxSlotted = CSVReader.GetInt(values[31]);
                item.maxSlottedCategoryCode = CSVReader.GetInt(values[32]);
                item.extraCapacity = CSVReader.GetInt(values[33]);
                item.rechargeRate = CSVReader.GetInt(values[34]);
                item.stealthDistance = CSVReader.GetInt(values[35]);
                item.cloakDistance = CSVReader.GetInt(values[36]);
                item.antiStealthDistance = CSVReader.GetInt(values[37]);
                item.antiCloakDistance = CSVReader.GetInt(values[38]);
                item.antiWarpDistance = CSVReader.GetInt(values[39]);
                item.stealthDistanceTime = CSVReader.GetInt(values[40]);
                item.cloakDistanceTime = CSVReader.GetInt(values[41]);
                item.vehicleTopSpeed = CSVReader.GetInt(values[42]);
                item.vehicleHyperTopSpeed = CSVReader.GetInt(values[43]);
                item.vehicleThrust = CSVReader.GetInt(values[44]);
                item.vehicleRotate = CSVReader.GetInt(values[45]);
                item.soccerThrowTimer = CSVReader.GetInt(values[46]);
                item.startMode = (StartMode)CSVReader.GetInt(values[47]);
                item.kineticIgnore = CSVReader.GetInt(values[52]);
                item.kineticPercent = CSVReader.GetInt(values[53]);
                item.explosiveIgnore = CSVReader.GetInt(values[54]);
                item.explosivePercent = CSVReader.GetInt(values[55]);
                item.electronicIgnore = CSVReader.GetInt(values[56]);
                item.electronicPercent = CSVReader.GetInt(values[57]);
                item.psionicIgnore = CSVReader.GetInt(values[58]);
                item.psionicPercent = CSVReader.GetInt(values[59]);
                item.bypassIgnore = CSVReader.GetInt(values[60]);
                item.bypassPercent = CSVReader.GetInt(values[61]);
                item.energyIgnore = CSVReader.GetInt(values[62]);
                item.energyPercent = CSVReader.GetInt(values[63]);
                item.losDistance = CSVReader.GetInt(values[124]);
                item.losAngle = CSVReader.GetInt(values[125]);
                item.losXray = CSVReader.GetInt(values[126]);
                item.losDistancePercent = CSVReader.GetInt(values[127]);
                item.losAnglePercent = CSVReader.GetInt(values[128]);
                item.combatAwareness = CSVReader.GetInt(values[129]);
                item.radarPrizeDistance = CSVReader.GetInt(values[130]);
                item.normalWeight = CSVReader.GetInt(values[131]);
                item.stopWeight = CSVReader.GetInt(values[132]);
                item.strafeThrust = CSVReader.GetInt(values[133]);
                item.forwardThrust = CSVReader.GetInt(values[134]);
                item.reverseThrust = CSVReader.GetInt(values[135]);
                item.soccerCatchRadius = CSVReader.GetInt(values[136]);
                return item;
            }

        }
    }
}