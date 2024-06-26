using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Multi
{
    public class CapturePoint
    {
        public string name;
        public short posX;
        public short posY;
        public int height;
        public int width;
        public Arena.FlagState _flag;
        public bool active;
        public bool _isBeingCaptured;
        private Arena _arena;
        private List<Arena.FlagState> _flags;
        private int tickLastWave;
        private int tickLastPointer;
        private int tickStartCapture;
        public event Action<Arena.FlagState> Captured;	//Called when the point has been captured
        private Script_Multi _baseScript;


        private List<Player> players;

        //Settings
        private int _flagCaptureRadius = 600;
        private int _flagCaptureTime = 5;


        public CapturePoint(Arena arena, Arena.FlagState flag, Script_Multi script)
        {
            _flag = flag;
            _arena = arena;
            players = new List<Player>();
            tickLastWave = 0;
            _flags = _arena._flags.Values.OrderBy(f => f.posX).ToList();
            _baseScript = script;

            posX = _flag.posX;
            posY = _flag.posY;

            name = Helpers.posToLetterCoord(posX, posY);
        }

        private void onCapture(Arena.FlagState flag)
        {

        }

        public void poll(int now)
        {
            List<Player> playersInArea = new List<Player>();
            int attackers = 0;
            int defenders = 0;

            Arena.FlagState flag = _flags.FirstOrDefault(f => f == _flag);

            playersInArea = _arena.getPlayersInRange(posX, posY, _flagCaptureRadius).Where(p => !p.IsDead).ToList();

            Team attacker = _arena.ActiveTeams.FirstOrDefault(t => t != flag.team);

            attackers = playersInArea.Count(p => p._team != flag.team);
            defenders = playersInArea.Count(p => p._team == flag.team);

            if (now - tickLastPointer >= 2000)
            {

                Helpers.ObjectState state = new Helpers.ObjectState();
                Helpers.ObjectState target = new Helpers.ObjectState();
                state.positionX = posX;
                state.positionY = posY;
                int index = _flags.IndexOf(flag);

                if (flag.team != null)
                {

                    if (flag.team._name == "Titan Militia")
                    {
                        if (index + 1 < _flags.Count)
                        {
                            target.positionX = _flags[index + 1].posX;
                            target.positionY = _flags[index + 1].posY;
                        }
                    }

                    if (flag.team._name == "Collective Military")
                    {
                        if (index - 1 >= 0)
                        {
                            target.positionX = _flags[index - 1].posX;
                            target.positionY = _flags[index - 1].posY;
                        }

                    }

                    byte fireAngle = Helpers.computeLeadFireAngle(state, target, 6500 / 1000);
                    Helpers.Player_RouteExplosion(flag.team.ActivePlayers, 1128, posX, posY, 0, fireAngle, 0);

                    tickLastPointer = now;
                }
            }

            if (attackers == 0 && defenders == 0)
            {
                tickStartCapture = 0;
            }

            if (attackers > defenders)
            {

                if (now - tickLastWave >= 2500)
                {
                    Helpers.Player_RouteExplosion(_arena.Players, 3059, posX, posY, 0, 0, 0);
                    tickLastWave = now;
                }
                
                if (tickStartCapture != 0 && attackers > 0)
                {
                    int quickCaptureMod = ((attackers - 1) * 1000);
                    tickStartCapture -= quickCaptureMod;
                }

                if (tickStartCapture != 0 && now - tickStartCapture >= 10000)
                {
                    _arena.triggerMessage(0, 500, String.Format("{0} has taken control of the {1} capture point...", attacker._name, name));
                    _flags.FirstOrDefault(f => f == flag).team = attacker;

                    foreach (Player player in playersInArea.Where(p => p._team == flag.team))
                        _baseScript.StatsCurrent(player).flagCaptures++;

                    //Call our capture event, if it exists
                    if (Captured != null)
                        Captured(_flag);

                    tickStartCapture = 0;
                }
                else if (tickStartCapture == 0)
                    tickStartCapture = now;
            }

            if (defenders > attackers)
            {
                if (now - tickLastWave >= 2500)
                {
                    //Helpers.Player_RouteExplosion(_arena.Players, 3059, posX, posY, 0, 0, 0);
                    tickLastWave = now;

                    tickStartCapture = 0;
                }
            }
            else
            {
                if (attackers == defenders && attackers > 0 && defenders > 0)
                {
                    if (now - tickLastWave >= 1500)
                    {
                        Helpers.Player_RouteExplosion(_arena.Players, 3060, posX, posY, 0, 0, 0);
                        tickLastWave = now;
                    }
                    tickStartCapture = 0;
                }
            }
        }
    }

}
