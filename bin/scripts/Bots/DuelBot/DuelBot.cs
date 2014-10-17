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

namespace InfServer.Script.DuelBot
{	// Script Class
	/// Provides the interface between the script and bot
	///////////////////////////////////////////////////////
	class Script_Duel : Scripts.IScript
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		private Bot _bot;							//Pointer to our bot class
		private Random _rand;

		private Player _victim;						//The player we're currently stalking

		private int _stalkRadius = 1000;			//The distance away we'll look for a victim (in pixels?)
		private int _optimalDistance = 100;		    //The optimal distance from the player we want to be
		private int _optimalDistanceTolerance = 20;	//The distance tolerance as we're moving back towards the player
		private int _distanceTolerance = 120;		//The tolerance from the optimal distance we accept

		private int _tickNextStrafeChange;			//The last time we changed strafe direction

		private bool _bStrafeLeft;					//Are we strafing left or right?
		private bool _bChasing;						//Are we chasing the player back into optimal range?


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

            //Maybe they want to assign custom settings?
            if(_bot._type.Description.Length >= 4 && _bot._type.Description.Substring(0,4).ToLower().Equals("bot="))
            {
                string[] botparams;
                botparams = _bot._type.Description.Substring(4, _bot._type.Description.Length - 4).Split(',');
                foreach (string botparam in botparams)
                {
                    if(!botparam.Contains(':'))
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
		{	//Reissue our instructions every poll
			_bot._movement.stop();

			//Get our current tick
			int tickCount = Environment.TickCount;

            //Are we dead?
            if (_bot.IsDead)
            {
                _bot.destroy(true);
                return false;
            }

            //Did we kill our victim?
            if (_victim != null && _victim.IsDead)
                //Taunt him!
                if (AssetManager.Manager.getItemByID(1095) != null)
                    _bot._itemUseID = 1095;

			//Is our victim still valid?
			if (_victim == null ||
				_victim.IsDead ||
				Helpers.distanceTo(_bot._state, _victim._state) > 1200)
				_victim = getClosestPlayer();

			if (_victim != null)
			{	//Are we close enough to start dueling?
				Vector2 distanceVector = new Vector2(
					_bot._state.positionX - _victim._state.positionX,
					_bot._state.positionY - _victim._state.positionY);
				double distance = distanceVector.Length;

				//Aim our weapon!
				bool bAimed;
				int aimResult = _bot._weapon.testAim(_victim._state, out bAimed);

				if (bAimed && _bot._weapon.ableToFire())
				{	//Spot on! Fire?
					_bot._itemUseID = _bot._weapon.ItemID;
					_bot._weapon.shotFired();

					//Don't want to spoil the aim!
					_bot._movement.stopRotating();
				}
				else if (aimResult > 0)
					_bot._movement.rotateRight();
				else
					_bot._movement.rotateLeft();

				if ((_bChasing && distance < (_optimalDistance + _distanceTolerance) &&
					distance > (_optimalDistance - _distanceTolerance))
					||
					(!_bChasing && distance < (_optimalDistance + _optimalDistanceTolerance) &&
					distance > (_optimalDistance - _optimalDistanceTolerance)))
				{	//We're in dueling range, and must no longer be chasing
					_bChasing = false;

					//Let's get some strafing going
					if (tickCount > _tickNextStrafeChange)
					{	//Strafe change sometime in the near future
						_tickNextStrafeChange = tickCount + _rand.Next(300, 1200);
						_bStrafeLeft = !_bStrafeLeft;
					}

					if (_bStrafeLeft)
						_bot._movement.strafeLeft();
					else
						_bot._movement.strafeRight();
				}
				else
					//We need to get in range!
					_bChasing = true;

				//Move towards him!
				double degrees = Helpers.calculateDegreesBetweenPoints(
					_victim._state.positionX, _victim._state.positionY,
					_bot._state.positionX, _bot._state.positionY);

				double difference = Helpers.calculateDifferenceInAngles(_bot._state.yaw, degrees);

				if (distance > _optimalDistance)
					_bot._movement.thrustForward();
				else
					_bot._movement.thrustBackward();
			}

			//Delegate updating to the controller
			return false;
		}

		/// <summary>
		/// Obtains the nearest valid player
		/// </summary>
		private Player getClosestPlayer()
		{	//Get a list of players in range
			List<Player> inTrackingRange =
				_bot._arena.getPlayersInRange(_bot._state.positionX, _bot._state.positionY, _stalkRadius);

			//Ignore dead and distant players
			inTrackingRange = inTrackingRange.Where(
				plyr => (plyr.IsDead == false && Helpers.distanceTo(_bot, plyr) < 1200)).ToList();

			if (inTrackingRange.Count == 0)
				return null;

			//Sort by distance
			inTrackingRange.Sort(
				delegate(Player p, Player q)
				{
					return Comparer<double>.Default.Compare(
						Helpers.distanceSquaredTo(_bot._state, p._state), Helpers.distanceSquaredTo(_bot._state, q._state));
				}
			);

			return inTrackingRange[0];
		}
	}
}