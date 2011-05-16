using System;
using System.Collections.Generic;
using System.Text;

using InfServer;
using InfServer.Game;
using InfServer.Protocol;

using Assets;

namespace InfServer.Bots
{
	// Bot Class
	/// Represents a bot, inherited from a vehicle
	///////////////////////////////////////////////////////
    public class Bot : Vehicle, IEventObject
	{	// Member variables
		///////////////////////////////////////////////////
        public new VehInfo.Car _type;				//The car type we represent
		public MovementController _movement;		//Our movement controller
		public WeaponController _weapon;			//Our trusty weapon controller

		public int _itemUseID;						//The item we're using

        private int _tickLastPoll;					//The last tick at which poll was called
		private int _tickLastUpdate;				//HACK: For stemming the flow of updates

		#region EventObject
		/// <summary>
		/// The event logger, if exists, for this class
		/// </summary>
		public EventHandlers events
		{
			get;
			set;
		}

		#region ThreadedObject
		/// <summary>
		/// The event logger, if exists, for this class
		/// </summary>
		public LogClient _eventLogger
		{
			get;
			set;
		}

		/// <summary>
		/// The sync object for this class
		/// </summary>
		public object _sync
		{
			get;
			set;
		}
		#endregion

		/// <summary>
		/// Initializes events for the event object
		/// </summary>
		public void eventInit(bool bParseEvents)
		{
			EventObjects.eventInit(this, bParseEvents);
		}

		/// <summary>
		/// Triggers an event
		/// </summary>
		public void trigger(string name, params object[] args)
		{
			EventObjects.trigger(this, name, true, args);
		}

		/// <summary>
		/// Calls a singlecast event, returning a value
		/// </summary>
		public object call(string name, params object[] args)
		{
			return EventObjects.callsync(this, name, true, args);
		}

		/// <summary>
		/// Calls a singlecast event, returning a value
		/// </summary>
		public object callsync(string name, bool bSync, params object[] args)
		{
			return EventObjects.callsync(this, name, bSync, args);
		}

		/// <summary>
		/// Determines if a event type exists
		/// </summary>
		public bool exists(string name)
		{	//Does the event exist?
			HandlerList list;
			return events.TryGetValue(name, out list);
		}

		/// <summary>
		/// Flushes the handlerlist - removing all handlers
		/// </summary>
		public void flushEvents()
		{	//Kill all the handlers!
			using (DdMonitor.Lock(_sync))
				events.Clear();
		}
		#endregion

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public Bot(VehInfo.Car type, Arena arena)
			: base(type, arena)
		{	//Initialize the event object
			eventInit(true);

			//Populate variables
			_type = type;

			_movement = new MovementController(_type, _state, arena);
			_weapon = new WeaponController(_state, new WeaponController.WeaponSettings());

			_tickLastPoll = Environment.TickCount;

			_bBotVehicle = true;
		}

		/// <summary>
		/// Generic constructor
		/// </summary>
        public Bot(VehInfo.Car type, Helpers.ObjectState state, Arena arena)
            : base(type, state, arena)
		{	//Initialize the event object
			eventInit(true);

			//Populate variables
            _type = type;

			_movement = new MovementController(_type, _state, arena);
			_weapon = new WeaponController(_state, new WeaponController.WeaponSettings());

			_tickLastPoll = Environment.TickCount;

			_bBotVehicle = true;
		}

		#region State
		/// <summary>
		/// The vehicle is being destroyed, clean up assets
		/// </summary>
		public override void destroy(bool bRestoreBase, bool bRemove)
		{	//Notify the arena of our destruction
			_arena.lostBot(this);
		}

		/// <summary>
		/// Looks after the bot's functionality
		/// </summary>
		public virtual bool poll()
        {	//Calculate our delta time..
			int tickCount = Environment.TickCount;
			int delta = tickCount - _tickLastPoll;
	
			//Don't need to update too much
			if (delta < 50)
				return true;

			_tickLastPoll = tickCount;

			//Allow our controller to update our vehicle state
			_movement.updateState(delta);

			if (_itemUseID == 0)
			{
				if (tickCount - _tickLastUpdate > 80)
					_tickLastUpdate = tickCount;
				else
					return false;
			}
			return true;
		}
		#endregion
    }
}
