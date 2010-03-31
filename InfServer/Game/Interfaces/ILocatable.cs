using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;

namespace InfServer.Game
{
	/// <summary>
	/// Interface for a class that can be spatially tracked
	/// </summary>
	public interface ILocatable
	{
		ushort getID();
		Helpers.ObjectState getState();
	}
}
