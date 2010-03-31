using System;
using System.Collections.Generic;
using System.Text;

using InfServer.Game;
using InfServer.Protocol;
using InfServer.Scripting;

using Assets;

namespace InfServer.Bots
{
	// ScriptBot Class
	/// Extends the bot class to allow scripting
	///////////////////////////////////////////////////////
	public class ScriptBot : Bot
	{	// Member variables
		///////////////////////////////////////////////////
		private List<Scripts.IScript> _scripts;		//The scripts we're currently supporting


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Generic constructor
		/// </summary>
		public ScriptBot(VehInfo.Car type, Arena arena, string scriptType)
			: base(type, arena)
		{
			loadScripts(scriptType);
		}

		/// <summary>
		/// Generic constructor
		/// </summary>
		public ScriptBot(VehInfo.Car type, Helpers.ObjectState state, Arena arena, string scriptType)
			: base(type, state, arena)
		{
			loadScripts(scriptType);
		}

		/// <summary>
		/// Loads scripts for the specified bot
		/// </summary>
		private void loadScripts(string scriptType)
		{
			_scripts = Scripts.instanceScripts(this, scriptType);
		}

		/// <summary>
		/// Looks after the bot's functionality
		/// </summary>
		public override bool poll()
		{	//Poll all scripts!
			bool bForce = false;

			foreach (Scripts.IScript script in _scripts)
				if (script.poll())
					bForce = true;

			//Allow the bot to function
			if (base.poll())
				return true;
			return bForce;
		}
	}
}
