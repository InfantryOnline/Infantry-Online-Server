﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Game;
using InfServer.Protocol;

using Assets;

using Axiom.Math;

namespace InfServer.Bots
{
	// MovementController Class
	/// Maintains the bot's object state based on simple movement instructions
	///////////////////////////////////////////////////////
    public class MovementController
	{	// Member variables
		///////////////////////////////////////////////////
		protected VehInfo.Car _type;					//The type of vehicle we represent
		protected Helpers.ObjectState _state;			//Our internal object state
		protected Arena _arena;                         //The arena we're in
		public bool bEnabled;							//Are we able to accept movement commands?
        public bool bCollision;
        public Vector3 _lastCollision;

        //Hardcode the terrain for now until we get access to the map info!
        private int _terrainType = 0;
		
		//Our internal representation of position/velocity/direction
		//These are vectors using float precision
		private Vector2 _position;
		private Vector2 _velocity;
		private double _direction;
		private double _thrustDirection;

		//Our vehicle movement statistics, we use these as they may be
		//changed due to various things in the game world
		private int _rollTopSpeed;
		private int _rollThrust;
		private int _rollThrustStrafe;
		private int _rollThrustBack;
		private double _rollFriction;
		private int _rollRotate;

		//Internals which define what the bot should do each update
		private bool _isThrustingForward = false;
		private bool _isThrustingBackward = false;
		private bool _isStrafingLeft = false;
		private bool _isStrafingRight = false;
		private bool _isRotatingLeft = false;
		private bool _isRotatingRight = false;

		private int _tickMovementFrozenUntil;

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic Constructor
		/// </summary>
        public MovementController(VehInfo.Car type, Helpers.ObjectState state, Arena arena)
        {
			_type = type;
			_state = state;
            _arena = arena;
			bEnabled = true;

			_position = new Vector2(state.positionX, state.positionY);
			_velocity = new Vector2(state.velocityX, state.velocityY);
			_direction = state.yaw;

			//Copy over the relevant vehicle values
			SpeedValues t0 = type.TerrainSpeeds[0];

			_rollFriction = t0.RollFriction;
			_rollThrust = t0.RollThrust * 4800 / 128;
			_rollThrustStrafe = t0.StrafeThrust * 4800 / 128;
			_rollThrustBack = t0.BackwardThrust * 4800 / 128;
			_rollRotate = t0.RollRotate;
			_rollTopSpeed = t0.RollTopSpeed;
        }

		/// <summary>
		/// Calculates the new angle direction based on a change
		/// </summary>
        public double calculateNewDirection(double direction, int change)
        {
            direction += change;
            direction %= 240;

            if (direction < 0)
                direction = 240 + direction;

            return direction;
        }

        /// <summary>
        /// Updates the state in accordance with movement instructions
        /// </summary>
        public virtual bool updateState(double delta)
        {   //If we're dead
            if (delta <= 0 || !bEnabled)
                return false;

            //Are we frozen?
            if (_tickMovementFrozenUntil != 0 && _tickMovementFrozenUntil > Environment.TickCount)
            {
                _isThrustingForward = false;
                _isThrustingBackward = false;
                _isStrafingLeft = false;
                _isStrafingRight = false;
            }
            else
                _tickMovementFrozenUntil = 0;

            //Get our speed values for our current terrain
            SpeedValues stats = _type.TerrainSpeeds[_terrainType];

            //Apply our rotation
            if (_isRotatingLeft)
                _direction -= ((_rollRotate / 10000.0d) * delta);
            else if (_isRotatingRight)
                _direction += ((_rollRotate / 10000.0d) * delta);

            _direction %= 240;

            if (_direction < 0)
                _direction = 240 + _direction;

            //Vectorize our direction, unf.
            Vector2 directionVector = Vector2.createUnitVector(_direction);
            Vector2 accelVector = Vector2.Zero;

            //Apply our thrusting instructions
            _thrustDirection = 0;

            if (_isThrustingForward)
            {
                accelVector.x += directionVector.x * _rollThrust;
                accelVector.y += directionVector.y * _rollThrust;
                _thrustDirection = _direction;
            }
            else if (_isThrustingBackward)
            {
                accelVector.x += directionVector.x * -_rollThrustBack;
                accelVector.y += directionVector.y * -_rollThrustBack;
                _thrustDirection = calculateNewDirection(_direction, 120);
            }

            if (_isStrafingLeft)
            {
                double tmpX = directionVector.x;
                accelVector.x += directionVector.y * _rollThrustStrafe;
                accelVector.y += -tmpX * _rollThrustStrafe;
            }
            else if (_isStrafingRight)
            {
                double tmpX = directionVector.x;
                accelVector.x += -directionVector.y * _rollThrustStrafe;
                accelVector.y += tmpX * _rollThrustStrafe;
            }

            //Apply our acceleration
            _velocity.x += accelVector.x * (delta / 1000.0d);
            _velocity.y += accelVector.y * (delta / 1000.0d);

            //Apply friction
            double frictionMod = 1000 - ((((((double)_rollFriction) * 800) / 256) / 10) * (delta / 10.0d));

            _velocity.x = ((_velocity.x * frictionMod) / 1000.0d);
            _velocity.y = ((_velocity.y * frictionMod) / 1000.0d);

            //TODO: Add the effects of projectile explosions on our velocity
            //TODO: Check for physics react accordingly

            //Clamp our resulting velocity
            if (_velocity.Length >= _rollTopSpeed / 1)
                _velocity.Normalize(_rollTopSpeed / 1);

            //Calculate our new position
            double xPerTick = _velocity.x / 10000.0d;
            double yPerTick = _velocity.y / 10000.0d;

            yPerTick *= 0.70f;          //Y coordinates are bigger than x by 0.7?

            Vector2 newPosition = new Vector2(_position.x + (xPerTick * delta), _position.y + (yPerTick * delta));

            //Clamp our position
            newPosition.x = Math.Min(newPosition.x, (AssetManager.Manager.Level.Width - 1) * 16);
            newPosition.x = Math.Max(newPosition.x, 0);
            newPosition.y = Math.Min(newPosition.y, (AssetManager.Manager.Level.Height - 1) * 16);
            newPosition.y = Math.Max(newPosition.y, 0);

            //Check for collisions
            int tileX = (int)Math.Floor(_position.x / 16);
            int tileY = (int)Math.Floor(_position.y / 16);
            int newTileX = (int)Math.Floor(newPosition.x / 16);
            int newTileY = (int)Math.Floor(newPosition.y / 16);

            LvlInfo.Tile tile = _arena._tiles[(newTileY * _arena._levelWidth) + newTileX];
            LvlInfo level = _arena._server._assets.Level;
            //Collision detection. Will expand on this later
            if (tile.Blocked && canRollOver((int)newPosition.x, (int)newPosition.y, _type))
            {
                bCollision = true;
                _lastCollision = new Vector3(((float)_position.x) / 100.0, ((float)newPosition.y) / 100.0, 0);

                if (Math.Abs(tileX - newTileX) > 0)
                    _velocity.x *= -1;

                if (Math.Abs(tileY - newTileY) > 0)
                    _velocity.y *= -1;

                _state.yaw = (byte)_direction;
                _state.direction = getDirection();

                //Apply any bounce physics
                _velocity *= _type.BouncePercent / 1000.0d;

                //Clamp our resulting velocity
                if (_velocity.Length >= _rollTopSpeed / 1)
                    _velocity.Normalize(_rollTopSpeed / 1);

                //Calculate our new position
                xPerTick = _velocity.x / 10000.0d;
                yPerTick = _velocity.y / 10000.0d;

                yPerTick *= 0.70f;          //Y coordinates are bigger than x by 0.7?

                newPosition = new Vector2(_position.x + (xPerTick * delta), _position.y + (yPerTick * delta));

                //Clamp our position
                newPosition.x = Math.Min(newPosition.x, (AssetManager.Manager.Level.Width - 1) * 16);
                newPosition.x = Math.Max(newPosition.x, 0);
                newPosition.y = Math.Min(newPosition.y, (AssetManager.Manager.Level.Height - 1) * 16);
                newPosition.y = Math.Max(newPosition.y, 0);

                _position.x = newPosition.x;
                _position.y = newPosition.y;

                _state.positionX = (short)(uint)_position.x;
                _state.positionY = (short)(uint)_position.y;
                _state.velocityX = (short)(uint)_velocity.x;
                _state.velocityY = (short)(uint)_velocity.y;
                _state.lastUpdate = Environment.TickCount;
                return false;
            }
            else
                bCollision = false;
            
            _position.x = newPosition.x;
            _position.y = newPosition.y;

            //Update the state, converting our floats into the nearest short values
            _state.positionX = (short)(uint)_position.x;
			_state.positionY = (short)(uint)_position.y;
			_state.velocityX = (short)(uint)_velocity.x;
			_state.velocityY = (short)(uint)_velocity.y;
			_state.yaw = (byte)_direction;
			_state.direction = getDirection();
			_state.lastUpdate = Environment.TickCount;

			//TODO: Apply vector tolerance to decide whether to update
			return true;
		}

		#region Movement Controls
		/// <summary>
		/// Causes the zombie to cease movement for the specified period of time
		/// </summary>		
		public void freezeMovement(int duration)
		{
			_tickMovementFrozenUntil = Environment.TickCount + duration;
		}

		/// <summary>
		/// Causes the bot to thrust forward
		/// </summary>
		public void thrustForward()
        {
            _isThrustingForward = true;
			_isThrustingBackward = false;
        }

		/// <summary>
		/// Causes the bot to thrust backwards
		/// </summary>
        public void thrustBackward()
        {
			_isThrustingForward = false;
            _isThrustingBackward = true;
        }

		/// <summary>
		/// Causes the bot to strafe left
		/// </summary>
        public void strafeLeft()
        {
            _isStrafingLeft = true;
			_isStrafingRight = false;
        }

		/// <summary>
		/// Causes the bot to strafe right
		/// </summary>
        public void strafeRight()
        {
			_isStrafingLeft = false;
            _isStrafingRight = true;
        }

		/// <summary>
		/// Stops all thrust movement
		/// </summary>
        public void stopThrusting()
        {
            _isThrustingForward = false;
            _isThrustingBackward = false;
        }

		/// <summary>
		/// Stops all strafe movement
		/// </summary>
        public void stopStrafing()
        {
            _isStrafingLeft = false;
            _isStrafingRight = false;
        }

		/// <summary>
		/// Causes the bot to rotate left
		/// </summary>
        public void rotateLeft()
        {
            _isRotatingLeft = true;
			_isRotatingRight = false;
        }

		/// <summary>
		/// Causes the bot to rotate right
		/// </summary>
        public void rotateRight()
        {
			_isRotatingLeft = false;
            _isRotatingRight = true;
        }

		/// <summary>
		/// Stops all rotational movement
		/// </summary>
        public void stopRotating()
        {
            _isRotatingLeft = false;
            _isRotatingRight = false;
        }

		/// <summary>
		/// Stops all movement completely
		/// </summary>
		/// <remarks>May take a while for the bot to come to a stop.</remarks>
        public void stop()
        {
            stopThrusting();
            stopStrafing();
            stopRotating();
		}
        #endregion

        #region Utility functions


        public bool canRollOver(int x, int y, VehInfo type)
        {
            LvlInfo level = _arena._server._assets.Level;

            bool blocked = level.isTileBlocked(x, y, type);
            if (blocked)
            {
                x /= 16;
                y /= 16;
                int phy = level.Tiles[y * level.Width + x].Physics;
                short low = level.PhysicsLow[phy];
                short high = level.PhysicsHigh[phy];

                //Typical man vehicle: LowZ = 0 HighZ = 55
                //Physic (Green): LowZ = 0 HighZ = 16

                //Can we pass under or over?
                if (_state.positionZ > low)
                    blocked = false;   //Not blocked
            }
            return blocked;
        }
        /// <summary>
        /// Determines the hardcoded direction value from our movement instructions
        /// </summary>
        public Helpers.ObjectState.Direction getDirection()
		{
			if (_isThrustingForward)
			{
				if (_isStrafingLeft)
					return Helpers.ObjectState.Direction.NorthWest;
				else if (_isStrafingRight)
					return Helpers.ObjectState.Direction.NorthEast;
				else
					return Helpers.ObjectState.Direction.Forward;
			}
			else if (_isThrustingBackward)
			{
				if (_isStrafingLeft)
					return Helpers.ObjectState.Direction.SouthWest;
				else if (_isStrafingRight)
					return Helpers.ObjectState.Direction.SouthEast;
				else
					return Helpers.ObjectState.Direction.Backward;
			}
			else if (_isStrafingLeft)
				return Helpers.ObjectState.Direction.StrafeLeft;
			else if (_isStrafingRight)
				return Helpers.ObjectState.Direction.StrafeRight;

			return Helpers.ObjectState.Direction.None;
		}
		#endregion
	}
}