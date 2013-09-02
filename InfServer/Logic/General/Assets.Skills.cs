using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using InfServer.Protocol;
using InfServer.Game;

using Assets;

namespace InfServer.Logic
{	// Logic_Assets Class
	/// Handles various asset-related functions
	///////////////////////////////////////////////////////
	public partial class Logic_Assets
	{
		static private Regex paramRegex = new Regex(@"\!?([%@#\-]?)([0-9]+)", RegexOptions.Compiled);
        static private Regex illiegalChars = new Regex(@"[^[!|()%@#&\-][0-9]+]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// The public skillcheck method
		/// </summary>		
		static public bool SkillCheck(Player player, string skillString)
		{
            // Any need?
            if (String.IsNullOrWhiteSpace(skillString) || skillString == "\"\"")
                return true;

            // Make the string safe to test
            String cleanString = illiegalChars.Replace(skillString, "");

            // Is there still any need?
            if (String.IsNullOrEmpty(cleanString))
				return true;

			// Compute ClassId for this case
			int classId = (player._occupiedVehicle == null) ?
				player._baseVehicle._type.ClassId : // Player is not in a car
				player._occupiedVehicle._type.ClassId; // Player is in a car

			return SkillCheckTester(player, classId, cleanString);
		}

        /// <summary>
        /// The public buildingcheck method
        /// </summary>
        static public bool BuildingCheck(Player player, string buildingString)
        {
            // We're done here.
            if (String.IsNullOrWhiteSpace(buildingString) || buildingString == "\"\"")
                return true;

            // Make the string safe to test
            String cleanString = illiegalChars.Replace(buildingString, "");

            // Is there still any need?
            if (String.IsNullOrEmpty(cleanString))
                return true;

            IEnumerable<Vehicle> vehs = player._arena.Vehicles.Where(v => v != null && v._team == player._team);

            //First, kill all spaces then replace with proper boolean values
            //The calculate boolean values
            String booleanString = paramRegex.Replace(cleanString.TrimStart('&'), delegate(Match m)
            {
                bool val = false;
                int numVal = int.Parse(m.Groups[1].Value);

                if (vehs != null)
                {
                    foreach (Vehicle veh in vehs)
                    {
                        if (veh._id == numVal)
                        {
                            val = true;
                            break;
                        }
                    }
                }
                return (val ^ m.Groups[0].Value.StartsWith("!")) ? "1" : "0";
            });

            int pos = 0;
            bool qualified = false;
            try
            {
                qualified = expr(booleanString, ref pos);
            }
            catch (ParseException e)
            {
                Log.write(TLog.Error, "Error parsing building string: '{0}', {1}", cleanString, e);
            }
            return qualified;
        }

		/// <summary>
		/// Determines whether a player satisifes a skill check
		/// </summary>
		static public bool SkillCheckTester(Player player, int classId, string skillString)
		{	// Get player's current experience - prefixed by '@' in the skill string for >= comparison
			int exp = player.Experience;

			// Get player's total Points - prefixed by '#' in the skill string for >= comparison
			long points = player.Points;
			
			// First, we kill all spaces (if any), then replace all the junk with proper boolean values.				
			// Then Calculate boolean values for all the shit in the expression.
			String booleanString = paramRegex.Replace(skillString.TrimStart('&'), delegate(Match m)
			{
				bool val;

				String prefix = m.Groups[1].Value;
				int numVal = int.Parse(m.Groups[2].Value);
				if (prefix == "%")
				{ // ClassId
					val = (classId == numVal) ? true : false;
				}
				else if (prefix == "@")
				{ // Experience
					val = (exp >= numVal) ? true : false;
				}
				else if (prefix == "#")
				{ // Points
					val = (points >= numVal) ? true : false;
				}
                else if (prefix == "-")
                { // Attributes
                    //Convert our values..
                    int attrVal = numVal % 1000;
                    int attrID = -numVal / 1000;
                    //Check em
                    if (player._skills.ContainsKey(attrID))
                    {
                        val = (player._skills[attrID].quantity >= attrVal) ? true : false;
                        player.syncState();
                    }
                    else { val = false; }
                }
				else
				{ // Skill (aaerox is super mean!)
					val = player._skills.ContainsKey(numVal);
				}

				return (val ^ m.Groups[0].Value.StartsWith("!")) ? "1" : "0";
			});

			int pos = 0;
			bool bQualified = false;
			try
			{
				bQualified = expr(booleanString, ref pos);
			}
			catch (Exception e)
			{
				Log.write(TLog.Error, "Error parsing skill string '{0}', {1}", skillString, e);
			}

			return bQualified;
		}

		#region Implementation of the recursive descent parser

        /* NOTE: For boolean evaluation, AND has higher precedence than OR when 
		 * no parentheses are used.
		 * 
		 * This is a simple implementation of a recursive descent parser
		 * where the NOT logic is already handled by above. Since the replaced
		 * string is all one character, the grammar is simple, and consists
		 * of the characters '0', '1', '|', '!', '&', '(', ')'.
		 * 
		 * expr			= and_expr ( '|' and_expr )*
		 * and_expr		= not_expr ( '&' not_expr )*
         * not_expr    = simple_expr | '!' not_expr
		 * simple_expr	= 0 | 1 | '(' expr ')'
		 */

        private static bool expr(String exp, ref int pos)
		{
			bool x = and_expr(exp, ref pos);
			while (pos < exp.Length && exp[pos] == '|')
			{
				pos++;
				x |= and_expr(exp, ref pos);
			}
			return x;
		}

		private static bool and_expr(String exp, ref int pos)
		{
			bool x = not_expr(exp, ref pos);
			while (pos < exp.Length && exp[pos] == '&')
			{
				pos++;
				x &= not_expr(exp, ref pos);
			}
			return x;
		}

        private static bool not_expr(String exp, ref int pos)
        {
            if (exp[pos] == '!')
            {
                pos++;
                return !not_expr(exp, ref pos);
            }
            else return simple_expr(exp, ref pos);                
        }

		private static bool simple_expr(String exp, ref int pos)
		{
			char c = exp[pos];
			if (c == '(')
			{
				pos++;
				bool x = expr(exp, ref pos);
				if (exp.Contains(')') != true ) throw new ParseException("Lacking closed parens");
				return x;
			}
            else if (c == '0' || c == '1')
            {
                pos++;
                return (c == '1') ? true : false;
            }
            else
                throw new ParseException("Unexpected character: "+c);
		}

		#endregion		

		class ParseException : Exception
		{
			public ParseException(string error) : base(error) { }
		}
	}
}