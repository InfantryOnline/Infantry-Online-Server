using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;

using Assets;

namespace InfServer.Game
{
	// Arena Class
	/// Represents a single arena in the server
	///////////////////////////////////////////////////////
	public partial class Arena : IChatTarget
	{	// Member variables
		///////////////////////////////////////////////////


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Looks after every computer vehicle in the game
		/// </summary>
		private void pollComputers()
		{	//Handle each computer vehicle
			int now = Environment.TickCount;

			foreach (Vehicle vehicle in _vehicles)
			{	//Cast it properly
				Computer computer = vehicle as Computer;
				if (computer == null)
					continue;

				//Is it time to remove it?
				if (computer._type.RemoveGlobalTimer != 0 &&
					now - computer._tickCreation > computer._type.RemoveGlobalTimer)
				{	//Destroy it and continue
					computer.destroy(false);
					continue;
				}

				//Does it need to send an update?
				if (computer.poll() || computer._sendUpdate)
				{	//Prepare and send an update packet for the vehicle
					Helpers.Update_RouteComputer(Players, computer);
				}					
			}
		}
	}
}
