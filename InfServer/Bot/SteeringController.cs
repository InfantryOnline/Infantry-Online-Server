using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Game;
using InfServer.Protocol;

using Assets;
using OpenSteerDotNet;
using Axiom.Math;


namespace InfServer.Bots
{
	// SteeringController Class
	/// Maintains the bot's object state based on simple movement instructions
	///////////////////////////////////////////////////////
	public class SteeringController : MovementController
	{	// Member variables
		///////////////////////////////////////////////////
		private SimpleVehicle vehicle;			//Our vehicle to model steering behavior

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic Constructor
		/// </summary>
		public SteeringController(VehInfo.Car type, Helpers.ObjectState state, Arena arena)
			: base(type, state, arena)
		{
			vehicle = new SimpleVehicle();
		}

		/// <summary>
		/// Updates the state in accordance with movement instructions
		/// </summary>
		public override bool updateState(double delta)
		{
			return base.updateState(delta);
		}

		#region Utility functions

		#endregion
	}
}