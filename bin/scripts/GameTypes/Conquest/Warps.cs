using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfServer.Script.GameType_CONQUEST
{
    public class Vector2
    {
        public int x;
        public int y;

        public Vector2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int DistanceTo(int x, int y)
        {
            return (int)Math.Sqrt(
                (Math.Pow(x, 2) - Math.Pow(this.x, 2)) + 
                (Math.Pow(y, 2) - Math.Pow(this.y, 2))
                );
        }
    }

    public class ConquestFlag
    {
        public Vector2 Position;
        public int TeamID;

        public ConquestFlag(Vector2 Position, int TeamID)
        {
            this.Position = Position;
            this.TeamID = TeamID;
        }
    }

    public class LowerLevel
    {
        //Warp spawn location
        public Vector2 LeftWarp = new Vector2(6596, 13000);
        public Vector2 MiddleWarp = new Vector2(8416, 14292);
        public Vector2 RightWarp = new Vector2(12312, 11920);

        
        //Flag Locations
        public Vector2 LeftFlagLocation = new Vector2(6384, 12737);
        public Vector2 MiddleFlagLocation = new Vector2(8544, 13553);
        public Vector2 RightFlagLocation = new Vector2(12208, 11281);

        //Flags that control warp switchs
        public ConquestFlag LeftFlag;
        public ConquestFlag MiddleFlag;
        public ConquestFlag RightFlag;

        //Titan warp switches
        public bool TitanLeftEnabled = true;
        public bool TitanMidEnabled = false;
        public bool TitanRightEnabled = false;

        //Collective warp switches
        public bool CollectiveLeftEnabled = false;
        public bool CollectiveMidEnabled = false;
        public bool CollectiveRightEnabled = true;

        public LowerLevel()
        {
            LeftFlag = new ConquestFlag(LeftFlagLocation, 2);
            MiddleFlag = new ConquestFlag(MiddleFlagLocation, 2);
            RightFlag = new ConquestFlag(RightFlagLocation, 2);

            TitanLeftEnabled = true;
            TitanMidEnabled = false;
            TitanRightEnabled = false;

            CollectiveLeftEnabled = false;
            CollectiveMidEnabled = false;
            CollectiveRightEnabled = true;
        }


        public void FlagChange(int TeamID, Vector2 Position)
        {
            if (TeamID == 0)
            {
                if (Position.x == RightFlagLocation.x &&
                    Position.y == RightFlagLocation.y)
                {

                    TitanRightEnabled = true;
                    
                    CollectiveRightEnabled = false;
                }
                else if (Position.x == MiddleFlagLocation.x &&
                    Position.y == MiddleFlagLocation.y)
                {
                    TitanMidEnabled = true;                   

                    CollectiveMidEnabled = false;
                }
                else if (Position.x == LeftFlagLocation.x &&
                    Position.y == LeftFlagLocation.y)
                {//Left flag
                    TitanLeftEnabled = true;

                    CollectiveLeftEnabled = false;
                }
            }
            else
            {
                if (Position.x == RightFlagLocation.x &&
                    Position.y == RightFlagLocation.y)
                {
                    TitanRightEnabled = false;

                    CollectiveRightEnabled = true;
                }
                else if (Position.x == MiddleFlagLocation.x &&
                    Position.y == MiddleFlagLocation.y)
                {
                    TitanMidEnabled = false;

                    CollectiveMidEnabled = true;
                }
                else if (Position.x == LeftFlagLocation.x &&
                    Position.y == LeftFlagLocation.y)
                {//Left flag
                    TitanLeftEnabled = false;

                    CollectiveLeftEnabled = true;
                }
            }            
        }


        public Vector2 GetNextWarp(int TeamID, int PortalID)
        {
            bool HandleWarp = false;
            switch (PortalID)
            {
                case 181:
                    HandleWarp = true;
                    break;
                case 176:
                    HandleWarp = true;
                    break;
                case 180:
                    HandleWarp = true;
                    break;
                case 183:
                    HandleWarp = true;
                    break;
                case 184:
                    HandleWarp = true;
                    break;
                case 182:
                    HandleWarp = true;
                    break;
            }
            if (!HandleWarp)
            {
                return null;
            }
            if (TeamID == 0)
            {
                if (TitanRightEnabled)
                {
                    return RightWarp;
                }
                else if (TitanMidEnabled)
                {
                    return MiddleWarp;
                }
                else if (TitanLeftEnabled)
                {
                    return LeftWarp;
                }
            }
            else if (TeamID == 1)
            {
                if (CollectiveLeftEnabled)
                {
                    return LeftWarp;
                }
                else if (CollectiveMidEnabled)
                {
                    return MiddleWarp;
                }
                else if (CollectiveRightEnabled)
                {
                    return RightWarp;
                }
            }
            return null;
        }
    }

    public class UpperLevel
    {
        //TODO
    }
}
