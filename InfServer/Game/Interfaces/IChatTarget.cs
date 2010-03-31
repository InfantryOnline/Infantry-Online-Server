using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Game;

namespace InfServer.Game
{
	// IChatTarget Interface
	/// Allows easy retrieval of chat targets from a social object
	///////////////////////////////////////////////////////
    public interface IChatTarget
    {
		/// <summary>        
		/// Returns the list of player targets for public chat
		/// </summary> 
        IEnumerable<Player> getChatTargets();
    }
}
