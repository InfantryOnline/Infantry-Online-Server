using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

using System.Reflection;
using CSScriptLibrary;

namespace InfServer.Scripting
{
	// ScriptParser Class
	/// Parses a .cs script file
	///////////////////////////////////////////////////////
	public class ScriptParser
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		public string m_filename;			//The filename of the script
		public string m_filepath;			//The full path to the file
		public string m_namespace;			//The namespace to replace with Bot

		public string m_preparedFile;		//The full path to the prepared (modified) source code file
		public bool m_bTemped;			//Was the source transferred to a temporary file?

		string m_code;						//The imported source code

		//Parsed data
		List<int[]> m_commentRegions;		//Regions inhabited by commented code [start, end]
		List<int[]> m_stringRegions;		//Regions inhabited by string definitions [start, end]
		public List<string> m_namespaces;	//The namespaces imported by this .cs script

		//Class for storing rename information
		class RenamingInfo
		{
			public int startPos;
			public int endPos;
			public string newValue;

			public RenamingInfo(int _startPos, int _endPos, string _newValue)
			{
				startPos = _startPos;
				endPos = _endPos;
				newValue = _newValue;
			}
		}


		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		#region Constructors
		//Constructor
		public ScriptParser(string szScript, bool bIsFile)
		{	//Initialize some variables
			m_commentRegions = new List<int[]>();
			m_stringRegions = new List<int[]>();
			m_namespaces = new List<string>();

			//If it's just code, parse it normally
			if (!bIsFile)
			{
				m_filename = "Code";
				initParser(szScript, "");
			}
			else
			{	//Otherwise, load it
				m_filename = Path.GetFileNameWithoutExtension(szScript);
				m_filepath = szScript;

				using (StreamReader sr = new StreamReader(szScript, Encoding.GetEncoding(0)))
					initParser(sr.ReadToEnd(), szScript);
			}
		}
		#endregion

		// initParser
		/// Initializes the parser and parses the given script 
		///////////////////////////////////////////////////
		public void initParser(string code, string filename)
		{	//Populate variables
			m_code = code;

			//Analyse comments and strings
			findCommentsAndStrings();

			//Find the end of the header area (where the class definitions begin)
			int classPos = m_code.IndexOf("class");
			int classEndPos = m_code.Length - 1;

			while (classPos != -1)
			{	//Is it the proper keyword?
				if (isToken(classPos, "class".Length) && !isComment(classPos))
				{	//We have it
					classEndPos = classPos;
					break;
				}

				//Continue searching
				classPos = m_code.IndexOf("class", classPos + 1);
			}

			//Analyse namespace references
			foreach (string statement in GetRawStatements("using", classEndPos, true))
				if (!statement.StartsWith("(")) //To cut off "using statements" as we are interested in "using directives" only
					m_namespaces.Add(statement.Trim().Replace("\t", "").Replace("\r", "").Replace("\n", "").Replace(" ", ""));
		}

		// prepare
		/// Prepares the script for compilation and writes to a file
		///////////////////////////////////////////////////
		public string prepare()
		{	//Rename namespaces if necessary
			string code = m_code;
			List<RenamingInfo> modifications = new List<RenamingInfo>();

			if (m_namespace != null)
				renameNamespaces(m_code, modifications, new string[][] { new string[] { "Bot", m_namespace } });

			//Rename any script classes
			renameScriptClasses(code, modifications);

			//If we have none to make, just return the unmodified file path
			if (modifications.Count == 0)
				return (m_preparedFile = m_filepath);

			//Sort the renaming positions in order of starting position
			modifications.Sort(
				delegate(RenamingInfo a, RenamingInfo b)
				{
					int result = (a == null) ? -1 : (b == null ? 1 : 0);

					if (result == 0)
						return Comparer.Default.Compare(a.startPos, b.startPos);
					else
						return result;
				});

			//Start building our new source code!
			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < modifications.Count; ++i)
			{	//Establish the end of the previous rename
				RenamingInfo info = modifications[i];
				int prevEnd = ((i - 1) >= 0) ? modifications[i - 1].endPos : 0;

				//Append all the code inbetween the previous rename and our current rename
				sb.Append(code.Substring(prevEnd, info.startPos - prevEnd));

				//Append our new value
				sb.Append(info.newValue);

				//If we're the last rename directive..
				if (i == modifications.Count - 1)
					//We need to add the rest of the code
					sb.Append(code.Substring(info.endPos, code.Length - info.endPos));
			}

			//Write the modified code into a temporary file
			string tempFile = Path.GetTempFileName();

			StreamWriter sw = new StreamWriter(tempFile);
			sw.Write(sb.ToString());
			sw.Flush();
			sw.Close();

			//We're done!
			return (m_preparedFile = tempFile);
		}

		// renameNamespaces
		/// Performs the renaming of namespaces as necessary 
		///////////////////////////////////////////////////
		private void renameNamespaces(string code, List<RenamingInfo> mods, string[][] renames)
		{	//Let's gather renaming information for each namespace
			foreach (string[] names in renames)
			{	//Find the next instance of the statement
				int renamingPos = -1;
				int pos = findStatement(names[0], 0);

				while (pos != -1 && renamingPos == -1)
				{	//Find the next namespace/class/whatever declaration
					int declarationStart = code.LastIndexOfAny("{};".ToCharArray(), pos, pos);

					do
					{	//Make sure it's actual code
						if (!isComment(declarationStart) || !isString(declarationStart))
						{	//Make sure it's in the "namespace [name]" format
							string statement = stripComments(code.Substring(declarationStart + 1, pos - declarationStart - 1));
							string[] tokens = statement.Trim().Split("\n\r\t ".ToCharArray());

							foreach (string token in tokens)
							{	//Locate the declaration start
								if (token.Trim() == "namespace")
								{
									renamingPos = pos;
									break;
								}
							}
							break;
						}
						else
							//If it was in a string or comments, look for the next occurance
							declarationStart = code.LastIndexOfAny("{};".ToCharArray(), declarationStart - 1, declarationStart - 1);
					}
					while (declarationStart != -1 && renamingPos == -1);

					//Look for the next statement
					pos = findStatement(names[0], pos + 1);
				}

				//Do we have an item to rename?
				if (renamingPos != -1)
					//Yes, declare it!
					mods.Add(new RenamingInfo(renamingPos, renamingPos + names[0].Length, names[1]));
			}
		}

		// renameScriptClasses
		/// Gives script classes a unique name if present 
		///////////////////////////////////////////////////
		private void renameScriptClasses(string code, List<RenamingInfo> mods)
		{	//Let's gather renaming information for each script class
			int renameNumber = 0;

			//Find the next script class
			int renamingPos = -1;
			int pos = findStatement("Script", 0);

			while (pos != -1 && renamingPos == -1)
			{	//Find the next namespace/class/whatever declaration
				int declarationStart = code.LastIndexOfAny("{};".ToCharArray(), pos, pos);

				//Valid?
				if (declarationStart == pos)
				{	//No, look for the next statement
					pos = findStatement("Script", pos + 1);
					continue;
				}

				do
				{	//Make sure it's actual code
					if (!isComment(declarationStart) && !isString(declarationStart) &&
						isToken(pos, "Script".Length))
					{	//Make sure it's in the "class [name]" format
						string statement = stripComments(code.Substring(declarationStart + 1, pos - declarationStart - 1));
						string[] tokens = statement.Trim().Split("\n\r\t ".ToCharArray());

						foreach (string token in tokens)
						{	//Locate the declaration start
							if (token.Trim() == "class")
							{
								renamingPos = pos;
								break;
							}
						}

						break;
					}
					else
						//If it was in a string or comments, look for the next occurance
						declarationStart = code.LastIndexOfAny("{};".ToCharArray(), declarationStart - 1, declarationStart - 1);
				}
				while (declarationStart != -1 && renamingPos == -1);

				//Do we have an item to rename?
				if (renamingPos != -1)
				{	//Yes, declare it!
					mods.Add(new RenamingInfo(renamingPos, renamingPos + "Script".Length, m_filename + "_" + (renameNumber++)));
					renamingPos = -1;
				}

				//Look for the next statement
				pos = findStatement("Script", pos + 1);
			}
		}

		#region Analysis functions
		// findCommentsAndStrings
		/// Finds comments and strings so that when parsing it done,
		/// the parser is able to tell where the comments and strings lie.
		///////////////////////////////////////////////////
		internal void findCommentsAndStrings()
		{	//Begin parsing
			List<int> quotationChars = new List<int>();

			int startPos = -1;
			int startSLC = -1;		//Single-line comment
			int startMLC = -1;		//Multi-line comment
			int searchOffset = 0;

			string endToken = "";
			string startToken = "";

			int endPos = -1;


			// Find comments
			///////////////////////////////////////////////
			do
			{	//Look for the next comment
				startSLC = m_code.IndexOf("//", searchOffset);
				startMLC = m_code.IndexOf("/*", searchOffset);

				//Handle the first occurance
				if (startSLC ==
					Math.Min((startSLC != -1 ? startSLC : Int16.MaxValue),
								(startMLC != -1 ? startMLC : Int16.MaxValue)))
				{	//Assign our tokens
					startPos = startSLC;
					startToken = "//";
					endToken = "\n";
				}
				else
				{	//It's a multiline comment, assign tokens
					startPos = startMLC;
					startToken = "/*";
					endToken = "*/";
				}

				//If we have a comment to parse, find the ending token
				if (startPos != -1)
					endPos = m_code.IndexOf(endToken, startPos + startToken.Length);

				//Did we find a valid comment?
				if (startPos != -1 && endPos != -1)
				{	//Start at the last identified comment region
					int startCode = (m_commentRegions.Count == 0 ? 0 : m_commentRegions[m_commentRegions.Count - 1][1] + 1);

					//Find all quotations between the start of the new comment
					//and the last comment.
					int[] quotations = allIndexOf("\"", startCode, startPos);

					//If it's uneven, something has gone wrong
					if ((quotations.Length % 2) != 0)
					{	//Resume
						searchOffset = startPos + startToken.Length;
						continue;
					}

					//Otherwise we're good - add our new comment region
					m_commentRegions.Add(new int[2] { startPos, endPos });
					quotationChars.AddRange(quotations);

					//Update our search offset
					searchOffset = endPos + endToken.Length;
				}

				//Loop until we find no more comments
			} while (startPos != -1 && endPos != -1);

			//If there was code left to search, add the remaining quotations
			if (searchOffset < m_code.Length)
				quotationChars.AddRange(allIndexOf("\"", searchOffset, m_code.Length));

			//Use the quotation locations to establish string regions
			for (int i = 0; i < quotationChars.Count; i += 2)
			{	//Add the region
				m_stringRegions.Add(new int[2] { quotationChars[i], quotationChars[i + 1] });
			}
		}

		// getStatements
		/// Finds all statements in the specified area using
		/// the specified keyword.
		///////////////////////////////////////////////////
		internal string[] GetRawStatements(string pattern, int endIndex, bool ignoreComments)
		{	//Look for the occurances of the pattern
			List<string> results = new List<string>();

			int pos = m_code.IndexOf(pattern);
			int endPos = -1;

			//Search until we can no longer
			while (pos != -1 && pos <= endIndex)
			{	//Is it a legitimate token?
				if (isToken(pos, pattern.Length))
				{	//If we're ignoring comments, test for commentdom
					if (!ignoreComments || (ignoreComments && !isComment(pos)))
					{	//Find the statement terminator
						pos += pattern.Length;
						endPos = m_code.IndexOf(";", pos);

						//Add the full statement
						if (endPos != -1)
							results.Add(m_code.Substring(pos, endPos - pos).Trim());
					}
				}

				//Search for more
				pos = m_code.IndexOf(pattern, pos + 1);
			}

			return results.ToArray();
		}
		#endregion

		#region Helper functions
		// findStatement
		/// Finds the first occurance of the given statement
		///////////////////////////////////////////////////
		internal int findStatement(string pattern, int start)
		{	//Take a look
			int pos = codeIndexOf(pattern, start, m_code.Length - 1);

			while (pos != -1)
			{	//If it isn't in a string declaration, we have it
				if (!isString(pos))
					return pos;
				else
					pos = codeIndexOf(pattern, pos + 1, m_code.Length - 1);
			}

			return -1;
		}

		// codeIndexOf
		/// Finds the next occurance of a value in the code
		/// ///////////////////////////////////////////////////
		internal int codeIndexOf(string pattern, int startIndex, int endIndex)
		{	//Check the code
			int pos = m_code.IndexOf(pattern, startIndex, endIndex - startIndex);

			while (pos != -1)
			{	//Is it a part of the code?
				if (!isComment(pos) && isToken(pos, pattern.Length))
					//Got it!
					return pos;

				//Otherwise, carry on searching
				pos = m_code.IndexOf(pattern, pos + 1, endIndex - (pos + 1));
			}

			return -1;
		}

		// allIndexOf
		/// Finds all indexes of a pattern occurance inside a specified area
		///////////////////////////////////////////////////
		internal int[] allIndexOf(string pattern, int startIndex, int endIndex)
		{	//Search until we can't find any more occurances
			List<int> result = new List<int>();

			int pos = m_code.IndexOf(pattern, startIndex, endIndex - startIndex);
			while (pos != -1)
			{
				result.Add(pos);
				pos = m_code.IndexOf(pattern, pos + 1, endIndex - (pos + 1));
			}

			return result.ToArray();
		}

		// isComment
		/// Is the specified character in a comment space?
		///////////////////////////////////////////////////
		bool isComment(int pos)
		{	//Compare with each region
			foreach (int[] region in m_commentRegions)
			{
				if (pos < region[0])
					return false;
				else if (region[0] <= pos && pos <= region[1])
					return true;
			}

			//We didn't find anything
			return false;
		}

		// isString
		/// Is the specified character in a string region?
		///////////////////////////////////////////////////
		bool isString(int pos)
		{	//Compare with each region
			foreach (int[] region in m_stringRegions)
			{
				if (pos < region[0])
					return false;
				else if (region[0] <= pos && pos <= region[1])
					return true;
			}

			//We didn't find anything
			return false;
		}

		// isToken
		/// Is the specified segment of text a single token?
		///////////////////////////////////////////////////
		bool isToken(int startPos, int length)
		{	//Sanity check
			if (m_code.Length < startPos + length)
				return false;

			//Attempt to obtain a string of the area
			int probeStart = (startPos != 0) ? startPos - 1 : 0;
			int endPos = (m_code.Length == startPos + length) ? startPos + length : startPos + length + 1;

			string original = m_code.Substring(startPos, length);
			string probeStr = m_code.Substring(probeStart, endPos - probeStart);

			//Remove all whitespaces from the string
			probeStr = probeStr.Replace(";", "").Replace("(", "").Replace(")", "").Replace("{", "");
			probeStr = probeStr.Trim();

			//And compare the length to see if it has changed
			return probeStr.Length == original.Length;
		}

		// stripComments
		/// Strips all comments from the specified text
		///////////////////////////////////////////////////
		string stripComments(string text)
		{	//Let's begin
			StringBuilder sb = new StringBuilder();
			int startPos = -1;
			int startSLC = -1;
			int startMLC = -1;
			int searchOffset = 0;
			string endToken = "";
			string startToken = "";
			int endPos = -1;
			int lastEndPos = -1;

			//Loop for every comment
			do
			{	//Obtain the next multi and single line comments
				startSLC = text.IndexOf("//", searchOffset);
				startMLC = text.IndexOf("/*", searchOffset);

				//Which is first?
				if (startSLC == Math.Min(startSLC != -1 ? startSLC : Int16.MaxValue,
										 startMLC != -1 ? startMLC : Int16.MaxValue))
				{
					startPos = startSLC;
					startToken = "//";
					endToken = "\n";
				}
				else
				{
					startPos = startMLC;
					startToken = "/*";
					endToken = "*/";
				}

				//If there's a comment to parse, find the ending
				if (startPos != -1)
					endPos = text.IndexOf(endToken, startPos + startToken.Length);

				//If it's valid comment (Start and end)..
				if (startPos != -1 && endPos != -1)
				{	//Append the code before the comment
					string codeFragment = text.Substring(searchOffset, startPos - searchOffset);
					sb.Append(codeFragment);

					//Start searching after the comment's end
					searchOffset = endPos + endToken.Length;
				}
			}
			while (startPos != -1 && endPos != -1);

			//If there's still text uncommitted
			if (lastEndPos != 0 && searchOffset < text.Length)
			{	//Commit it..
				string codeFragment = text.Substring(searchOffset, text.Length - searchOffset);
				sb.Append(codeFragment);
			}

			//Done!
			return sb.ToString();
		}
		#endregion
	}
}
