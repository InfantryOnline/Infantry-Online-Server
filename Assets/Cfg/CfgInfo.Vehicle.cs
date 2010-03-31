using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Vehicle
        {
            public int physicsDelay;
            public int warpDamageIgnoreTime;
            public int hitTriggerDelay;
            public int warpGetInDelay;
            public bool computerProduceInParallel;
            public bool computerExplosionDamageComputer;
            public int explodeItemDistance;
            public int inheritSpeedOnDefaultChange;
            public int energyOnDefaultChange;
            public int unownedPositionUseTeamColor;
            public bool computerProduceLocation;
            public int computerProduceRadius;
            public bool computerProduceForceDriver;

            public Vehicle(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Vehicle"];

                physicsDelay = Parser.GetInt("PhysicsDelay");
                warpDamageIgnoreTime = Parser.GetInt("WarpDamageIgnoreTime");
                hitTriggerDelay = Parser.GetInt("HitTriggerDelay");
                warpGetInDelay = Parser.GetInt("WarpGetInDelay");
                computerProduceInParallel = Parser.GetBool("ComputerProduceInParallel");
                computerExplosionDamageComputer = Parser.GetBool("ComputerExplosionDamageComputer");
                explodeItemDistance = Parser.GetInt("ExplodeItemDistance");
                inheritSpeedOnDefaultChange = Parser.GetInt("InheritSpeedOnDefaultChange");
                energyOnDefaultChange = Parser.GetInt("EnergyOnDefaultChange");
                unownedPositionUseTeamColor = Parser.GetInt("UnownedPositionUseTeamColor");
                computerProduceLocation = Parser.GetBool("ComputerProduceLocation");
                computerProduceRadius = Parser.GetInt("ComputerProduceRadius");
                computerProduceForceDriver = Parser.GetBool("ComputerProduceForceDriver");
            }
        }
    }
}
