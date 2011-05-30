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
		private Dictionary<string, object> _customData = new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);

		/// <summary>        
		/// Sets a custom variable, used for scripting
		/// </summary> 
		public void setVar(string name, object value)
		{
			_customData[name] = value;
		}

		/// <summary>        
		/// Retrieves a custom variable
		/// </summary> 
		public object getVar(string name)
		{
			object value;

			if (!_customData.TryGetValue(name, out value))
				return null;

			return value;
		}

		/// <summary>        
		/// Retrieves a string custom variable
		/// </summary> 
		/// <remarks>Returns "" if the string doesn't exist, or object isn't a string</remarks>
		public string getVarString(string name)
		{
			object value;

			if (!_customData.TryGetValue(name, out value))
				return "";

			if (!(value is string))
				return "";

			return value as string;
		}

		/// <summary>        
		/// Retrieves an integer custom variable
		/// </summary> 
		/// <remarks>Returns 0 if the integer doesn't exist</remarks>
		public int getVarInt(string name)
		{
			object value;

			if (!_customData.TryGetValue(name, out value))
				return 0;

			return Convert.ToInt32(value);
		}

		/// <summary>        
		/// Clears all customobject variables from the object
		/// </summary> 
		public void resetVars()
		{
			_customData.Clear();
		}
	}
}
