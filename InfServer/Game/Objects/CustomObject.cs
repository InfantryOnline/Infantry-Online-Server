using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfServer.Game
{
	/// <summary>
	/// Allows an object to retain custom values for scripting purposes
	/// </summary>
	public class CustomObject
	{
		private Dictionary<string, object> _customData = new Dictionary<string,object>();

		/// <summary>        
		/// Sets a custom variable, used for scripting
		/// </summary> 
		public void setVar(string name, object value)
		{
			_customData[name.ToLower()] = value;
		}

		/// <summary>        
		/// Retrieves a custom variable
		/// </summary> 
		public object getVar(string name)
		{
			object value;

			if (!_customData.TryGetValue(name.ToLower(), out value))
				return null;

			return value;
		}

		/// <summary>        
		/// Retrieves a string custom variable
		/// </summary> 
		public string getVarString(string name)
		{
			object value;

			if (!_customData.TryGetValue(name.ToLower(), out value))
				return "";

			if (!(value is string))
				return "";

			return value as string;
		}

		/// <summary>        
		/// Retrieves an integer custom variable
		/// </summary> 
		public int getVarInt(string name)
		{
			object value;

			if (!_customData.TryGetValue(name.ToLower(), out value))
				return 0;

			return Convert.ToInt32(value);
		}
	}
}
