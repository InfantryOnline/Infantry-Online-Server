using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfServer.Network
{
	// IClient Interface
	/// Provides the interface necessary to act as a client object
	///////////////////////////////////////////////////////
	public interface IClient
	{
		/// <summary>
		/// Allows our client object to be destroyed properly
		/// </summary>
		void destroy();
	}
}
