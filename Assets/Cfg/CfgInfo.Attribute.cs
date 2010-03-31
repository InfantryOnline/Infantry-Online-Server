using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Attribute
        {
            public int health;
            public int healthRatio;
            public int energy;
            public int energyRatio;
            public int weight;
            public int weightRatio;
            public int thrust;
            public int thrustRatio;
            public int topSpeed;
            public int topSpeedRatio;

            public Attribute(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Attribute"];

                health = Parser.GetInt("Health");
                healthRatio = Parser.GetInt("HealthRatio");
                energy = Parser.GetInt("Energy");
                energyRatio = Parser.GetInt("EnergyRatio");
                weight = Parser.GetInt("Weight");
                weightRatio = Parser.GetInt("WeightRatio");
                thrust = Parser.GetInt("Thrust");
                thrustRatio = Parser.GetInt("ThrustRatio");
                topSpeed = Parser.GetInt("TopSpeed");
                topSpeedRatio = Parser.GetInt("TopSpeedRatio");
            }
        }
    }
}
