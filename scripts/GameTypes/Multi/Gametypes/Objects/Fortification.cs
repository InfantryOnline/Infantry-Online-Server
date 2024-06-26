using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Multi
{ 
    public class Fortification
    {
        public Helpers.ObjectState _state;
        public FortificationType _type;
        public Team _team;
        private Arena _arena;

        public Fortification(FortificationType type, short positionX, short positionY, Team team, Arena arena)
        {
            _state = new Helpers.ObjectState();
            _state.positionX = positionX;
            _state.positionY = positionY;
            _team = team;
            _arena = arena;

            switch (type)
            {
                case FortificationType.Light:
                    {
                        spawnLightFort();
                        break;
                    }
            }

        }
        public void spawnLightFort()
        {
            Helpers.ObjectState objState = new Helpers.ObjectState();
            short pX, pY;

            pX = (short)(_state.positionX - 175);
            pY = (short)(_state.positionY - 175);
            objState = getRandomPosition(pX, pY, 200);
            _arena.newVehicle(Types.barricade, _team, null, objState);

            pX = (short)(_state.positionX - 175);
            pY = (short)(_state.positionY + 175);
            objState = getRandomPosition(pX, pY, 200);
            _arena.newVehicle(Types.barricade, _team, null, objState);

            pX = (short)(_state.positionX);
            pY = (short)(_state.positionY + 250);
            objState = getRandomPosition(pX, pY, 200);
            _arena.newVehicle(Types.bunkerMarine, _team, null, objState);

            pX = (short)(_state.positionX);
            pY = (short)(_state.positionY - 250);
            objState = getRandomPosition(pX, pY, 200);
            _arena.newVehicle(Types.bunkerRipper, _team, null, objState);
        }

        public Helpers.ObjectState getRandomPosition(short posX, short posY, int radius)
        {
            Helpers.ObjectState result = new Helpers.ObjectState();
            int blockedAttempts = 35;
            short pX;
            short pY;
            while (true)
            {
                pX = posX;
                pY = posY;
                Helpers.randomPositionInArea(_arena, radius, ref pX, ref pY);
                if (_arena.getTile(pX, pY).Blocked)
                {
                    blockedAttempts--;
                    if (blockedAttempts <= 0)
                        //Consider the spawn to be blocked
                        return null;
                    continue;
                }
                if (isAreaBlocked(pX, pY))
                {
                    blockedAttempts--;
                    if (blockedAttempts <= 0)
                        //Consider the spawn to be blocked
                        return null;
                    continue;
                }

                result.positionX = pX;
                result.positionY = pY;

                return result;
            }
        }

        public bool isAreaBlocked(short posX, short posY)
        {
            //Find a clear location
            if (!_arena.getUnblockedTileInRadius(ref posX, ref posY, 100, 100, 100))
                return false;

            return true;
        }
    }

    public enum FortificationType
    {
        Light,
        Medium,
        Heavy
    }

    public abstract class Types
    {
        public static VehInfo barricade = AssetManager.Manager.getVehicleByID(701);
        public static VehInfo bunkerRipper = AssetManager.Manager.getVehicleByID(400);
        public static VehInfo bunkerMarine = AssetManager.Manager.getVehicleByID(401);
    }
   
}
