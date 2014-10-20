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

using Axiom.Math;

namespace InfServer.Script.TeamBot
{   // Script Class
    /// Provides the interface between the script and bot
    ///////////////////////////////////////////////////////
    class Script_Team : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Bot _bot;						    //Pointer to our bot class
        private List<Assets.ItemInfo> _weapons;     //The list of weapons we have on us
        private Player _teamMember;                 //The closet team member we are following
        private Player _victim;                     //The player we are chasing

        private Random _rand;
        private int _stalkRadius = 1200;            //The distance to follow our team
        private int _optimalDistance = 100;         //The optimal distance from the player we want to be
        private int _optimalDistanceTolerance = 30; //The distance tolerance as we move back to the player
        private int _distanceTolerance = 120;       //The tolerance based on the optimal we accept
        private int _tickNextStrafeChange;          //The last time we changed strafing direction

        private bool _strafeLeft;                   //Are we strafing left or right?
        private bool _chasing;                      //Are we chasing the player back into optimal range?
        private int _tickLastShot;
        private int _tickLastKnife;

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////
        /// <summary>
        /// Performs script initialization
        /// </summary>
        public bool init(IEventObject invoker)
        {	//Populate our variables
            _bot = invoker as Bot;
            _rand = new Random();

            //Equip and fix up our bot! :)
            _bot._weapon.equip(AssetManager.Manager.getItemByID(_bot._type.InventoryItems[0]));

            //Construct our weapons list
            _weapons = new List<Assets.ItemInfo>();
            Assets.ItemInfo item;
            foreach (int id in _bot._type.InventoryItems)
            {
                item = AssetManager.Manager.getItemByID(id);
                if (item != null)
                    _weapons.Add(item);
            }

            if (_bot._type.Description.Length >= 4 && _bot._type.Description.Substring(0, 4).ToLower().Equals("bot="))
            {
                string[] botparams;
                botparams = _bot._type.Description.Substring(4, _bot._type.Description.Length - 4).Split(',');
                foreach (string botparam in botparams)
                {
                    if (!botparam.Contains(':'))
                        continue;

                    string paramname = botparam.Split(':').ElementAt(0).ToLower();
                    string paramvalue = botparam.Split(':').ElementAt(1).ToLower();
                    switch (paramname)
                    {
                        case "radius":
                            _stalkRadius = Convert.ToInt32(paramvalue);
                            break;

                        case "distance":
                            _optimalDistance = Convert.ToInt32(paramvalue);
                            break;

                        case "tolerance":
                            _optimalDistanceTolerance = Convert.ToInt32(paramvalue);
                            break;

                        case "strafe":
                            _distanceTolerance = Convert.ToInt32(paramvalue);
                            break;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Allows the script to maintain itself
        /// </summary>
        public bool poll()
        {
            //Reissue our instructions each poll
            _bot._movement.stop();

            //We dead?
            if (_bot.IsDead)
            {
                _bot.destroy(true);
                return false;
            }

            int tickCount = Environment.TickCount;

            //Move to the closet team member
            _teamMember = getClosetTeamMember();
            if (_teamMember != null)
            {
                Assets.ItemInfo medkit = _weapons.First(w => w.itemType == Assets.ItemInfo.ItemType.Repair
                            && w.name.ToLower().Equals("medikit"));
                if (medkit != null)
                {
                    Assets.ItemInfo.RepairItem rep = medkit as Assets.ItemInfo.RepairItem;

                    List<Player> healRange = _bot._arena.getPlayersInRange(_bot._state.positionX, _bot._state.positionY, _stalkRadius);
                    healRange = healRange.Where(p => p.IsDead == false && p._team == _bot._team).ToList();

                    int repDistance = rep.repairDistance;
                    if (repDistance < 0)
                        repDistance = (repDistance * (-1));
                    //Check players hp on the team
                    foreach (Player p in healRange)
                    {
                        if (p._state.health < 75 && _bot._weapon.ableToFire())
                        {   //Someone's hurt, healing takes priority first!
                            MoveTowardsPlayer(p, false);

                            //Recheck if we are close enough
                            if (!Helpers.isInRange(repDistance, p._state, _bot._state))
                            {
                                Console.WriteLine("Redo");
                                //We arent, start over
                                return false;
                            }

                            //We're close enough, heal!
                            if (Environment.TickCount - _tickLastShot > (rep.fireDelay * 10))
                            {
                                Console.WriteLine("Should fire {0}", rep.id.ToString());
                                _bot._itemUseID = rep.id;
                                _tickLastShot = Environment.TickCount;
                            }
                        }
                    }
                }

                //Lets follow now
                MoveTowardsPlayer(_teamMember, false);
            }

            //Move to the closet enemy
            if (_victim == null || _victim.IsDead
                || Helpers.distanceTo(_bot._state, _victim._state) > _stalkRadius)
                _victim = getClosetPlayer();

            if (_victim != null)
            {   //Are we close enough to shoot at?
                Vector2 distanceVector = new Vector2(_bot._state.positionX - _victim._state.positionX, _bot._state.positionY - _victim._state.positionY);
                double victim_distance = distanceVector.Length;

                Vector2 distVector = new Vector2(_bot._state.positionX - _teamMember._state.positionX, _bot._state.positionY - _teamMember._state.positionY);
                double team_distance = distVector.Length;

                //Aim our weapon
                bool bAim;
                int aimResult = _bot._weapon.testAim(_victim._state, out bAim);

                if (bAim && _bot._weapon.ableToFire())
                {   //Ready to FIRE!
                    double distanceTo = Helpers.distanceTo(_bot._state, _victim._state);

                    //Should we use sg?
                    if (distanceTo <= 200)
                    {
                        if (distanceTo > 35) //Knife range
                        {
                            Assets.ItemInfo weap1 = _weapons.First(w => w.itemType == Assets.ItemInfo.ItemType.MultiUse
                                && w.name.ToLower().Equals("shotgun"));
                            Assets.ItemInfo weap2 = _weapons.First(w => w.itemType == Assets.ItemInfo.ItemType.Projectile
                                && w.name.ToLower().Equals("shotgun"));
                            if (weap1 != null)
                            {
                                //Equip and fix up our bot!
                                _bot._weapon.equip(AssetManager.Manager.getItemByID(weap1.id));
                                _bot._itemUseID = _bot._weapon.ItemID;
                                _bot._weapon.shotFired();
                            }
                            else if (weap2 != null)
                            {
                                //Equip and fix up our bot!
                                _bot._weapon.equip(AssetManager.Manager.getItemByID(weap2.id));
                                _bot._itemUseID = _bot._weapon.ItemID;
                                _bot._weapon.shotFired();
                            }
                        }
                        else
                        {   //Knife em!
                            Assets.ItemInfo knife = _weapons.First(w => w.itemType == Assets.ItemInfo.ItemType.Projectile
                                && w.name.ToLower().Equals("combat knife"));
                            if (knife != null)
                            {
                                //Equip and fire
                                //We're close enough, shoot at them!
                                if (Environment.TickCount - _tickLastKnife > 1200)
                                {
                                    _bot._itemUseID = knife.id;
                                    _tickLastKnife = Environment.TickCount;
                                }
                            }
                        }
                    }
                    else
                    {
                        //Double check to see if our main is still equipped
                        if (_bot._weapon.ItemID != _bot._type.InventoryItems[0])
                            //Its not, re-equip
                            _bot._weapon.equip(AssetManager.Manager.getItemByID(_bot._type.InventoryItems[0]));
                        _bot._weapon.shotFired();
                    }

                    //Don't want to spoil our aim!
                    _bot._movement.stopRotating();
                }
                else if (aimResult > 0)
                    _bot._movement.rotateRight();
                else
                    _bot._movement.rotateLeft();

                if ((_chasing && victim_distance < (_optimalDistance + _distanceTolerance) &&
                    victim_distance > (_optimalDistance - _distanceTolerance))
                    || (!_chasing && victim_distance < (_optimalDistance + _optimalDistanceTolerance) &&
                    victim_distance > (_optimalDistance - _optimalDistanceTolerance)))
                {   //We're in dueling range
                    _chasing = false;
                }
                else
                    _chasing = true;

                //Let's get some strafing going
                if (tickCount > _tickNextStrafeChange)
                {	//Strafe change sometime in the near future
                    _tickNextStrafeChange = tickCount + _rand.Next(300, 1200);
                    _strafeLeft = !_strafeLeft;
                }

                if (_strafeLeft)
                    _bot._movement.strafeLeft();
                else
                    _bot._movement.strafeRight();

                double victim_degrees = Helpers.calculateDegreesBetweenPoints(
                    _victim._state.positionX, _victim._state.positionY,
                    _bot._state.positionX, _bot._state.positionY);
                double victim_difference = Helpers.calculateDifferenceInAngles(_bot._state.yaw, victim_degrees);

                //Are we allowed to move forward?
                if (_teamMember == null)
                {   //Team member must have died, get a kill
                    if (victim_distance > _optimalDistance)
                        _bot._movement.thrustForward();
                    else
                        _bot._movement.thrustBackward();
                }
                else
                {
                    if (team_distance > _optimalDistance)
                        _bot._movement.thrustForward();
                    else if (team_distance < _optimalDistance)
                        _bot._movement.thrustBackward();
                }

                if (Math.Abs(victim_difference) < 5)
                    _bot._movement.stopRotating();
                else if (victim_difference > 0)
                    _bot._movement.rotateRight();
                else
                    _bot._movement.rotateLeft();
            }

            return false;
        }

        /// <summary>
        /// Obtains the nearest valid player
        /// </summary>
        private Player getClosetPlayer()
        {   //Get a list of players within range
            List<Player> trackingRange = _bot._arena.getPlayersInRange(_bot._state.positionX, _bot._state.positionY, _stalkRadius);

            //Ignore dead people
            trackingRange = trackingRange.Where(plyr => plyr.IsDead == false && _bot._team != plyr._team).ToList();

            if (trackingRange.Count == 0)
                return null;

            //Sort by distance
            trackingRange.Sort(
                delegate(Player p, Player q)
                {
                    return Comparer<double>.Default.Compare(Helpers.distanceSquaredTo(_bot._state, p._state), Helpers.distanceSquaredTo(_bot._state, q._state));
                }
            );

            return trackingRange[0];
        }

        /// <summary>
        /// Obtains the nearest valid team mate
        /// </summary>
        private Player getClosetTeamMember()
        {
            List<Player> trackingRange = _bot._arena.getPlayersInRange(_bot._state.positionX, _bot._state.positionY, _stalkRadius);

            //Ignore dead people and non team members
            trackingRange = trackingRange.Where(plyr => plyr.IsDead == false && _bot._team == plyr._team).ToList();

            if (trackingRange.Count == 0)
                return null;

            //Sort by distance
            trackingRange.Sort(
                delegate(Player p, Player q)
                {
                    return Comparer<double>.Default.Compare(
                        Helpers.distanceSquaredTo(_bot._state, p._state), Helpers.distanceSquaredTo(_bot._state, q._state));
                }
            );

            return trackingRange[0];
        }

        /// <summary>
        /// Moves towards an enemy or teammate
        /// </summary>
        private void MoveTowardsPlayer(Player player, bool attacking)
        {
            if (player == null)
            {
                if (!attacking)
                    //Pick a new team to move to
                    player = getClosetTeamMember();
                else
                    //Pick a new enemy to move to
                    player = getClosetPlayer();
            }

            //Move towards him
            double degrees = Helpers.calculateDegreesBetweenPoints(player._state.positionX, player._state.positionY,
                _bot._state.positionX, _bot._state.positionY);

            double difference = Helpers.calculateDifferenceInAngles(_bot._state.yaw, degrees);
            if (Math.Abs(difference) < 5)
                _bot._movement.stopRotating();
            else if (difference > 0)
                _bot._movement.rotateRight();
            else
                _bot._movement.rotateLeft();

            Vector2 distanceVector = new Vector2(_bot._state.positionX - player._state.positionX, _bot._state.positionY - player._state.positionY);
            _bot._movement.stopThrusting();
            Real distLength = distanceVector.Length;

            if (distLength > 120)
                _bot._movement.thrustForward();
            else if (distLength < 50)
                _bot._movement.thrustBackward();
        }
    }
}