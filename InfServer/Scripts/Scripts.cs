using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;

using CSScriptLibrary;

namespace InfServer.Scripting
{
	// Scripts Class
	/// Encompasses the script loading and execution functionality
	///////////////////////////////////////////////////////
	public class Scripts
	{	///////////////////////////////////////////////////
		// Member Variables
		///////////////////////////////////////////////////
		//Invoker type to scripting objects dictionary
		static private Dictionary<string, List<Type>>
			_invokerScripts = new Dictionary<string, List<Type>>();

		//The assembly of compiled scripts
		static private Assembly _scripts;

		//Bot types
		static private List<InvokerType>
			_invokerTypes = new List<InvokerType>();

		#region Member Classes
		/// <summary>
		/// Provides a simple interface for scripting objects
		/// </summary>
		public interface IScript
		{	//init - Allows the script to register events, etc.
			bool init(IEventObject invoker);

			//poll - Allows the script to take care of matters
			bool poll();
		}

		/// <summary>
		/// Used for declaring event functions within scripts
		/// </summary>
		public class Event : Attribute
		{	//Variables
			public string eventname;	//Name of the associated event

			public Event(string name)
			{
				eventname = name;
			}
		}

		/// <summary>
		/// Houses a list of unique assembly locations
		/// </summary>
		class UniqueAssemblyLocations
		{	//Conversion to string[]
			public static explicit operator string[](UniqueAssemblyLocations obj)
			{
				string[] retval = new string[obj.locations.Count];
				obj.locations.Values.CopyTo(retval, 0);
				return retval;
			}

			//Attempt to add an assembly
			public void AddAssembly(string location)
			{
				try
				{	//Get the path name
					string assemblyID = Path.GetFileName(location);

					//Does this path already exist?
					if (!locations.ContainsKey(assemblyID))
						//No? assign it
						locations[assemblyID] = location;
				}
				catch (ArgumentException)
				{	//Bogus pathname
				}

			}
			System.Collections.Hashtable locations = new System.Collections.Hashtable();
		}
		#endregion Subclasses

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		#region Runtime Functions
		/// <summary>
		/// Determines whether an invoker type exists
		/// </summary>
		static public bool invokerTypeExists(string invokerType)
		{
			return _invokerScripts.ContainsKey(invokerType);
		}

		/// <summary>
		/// Instances all available scripts for an invoker
		/// </summary>
		static public List<IScript> instanceScripts(IEventObject invoker, string invokerType)
		{	//Get our scripts
			List<Type> scripts;

			if (!_invokerScripts.TryGetValue(invokerType, out scripts))
				//Invalid bot type!
				throw new ArgumentException("Invalid invoker type for instancing: " + invokerType);

			//Instance them all!
			int instanced = 0;
			List<IScript> iscripts = new List<IScript>();

			foreach (Type script in scripts)
			{
				IScript iscript = (IScript)Activator.CreateInstance(script);

				//Now allow it to initialize
				bool success = iscript.init(invoker);
				if (!success)
					continue;

				instanced++;

				//Look though all the methods present
				foreach (MethodInfo method in script.GetMethods())
				{	//Look at it's attributes..
					foreach (Attribute attr in method.GetCustomAttributes(true))
					{	//.. until we find event attribute
						Event ehdlr = attr as Event;

						if (ehdlr != null)
						{	//Register the event
							invoker.events[ehdlr.eventname] += new CEventHandler(iscript, method.Name);
						}
					}
				}

				//Add it to our list
				iscripts.Add(iscript);
			}

			Log.write(TLog.Inane, "Instanced " + instanced + " '" + invokerType + "' scripts.");
			return iscripts;
		}
		#endregion Runtime Functions

		#region Compilation
		/// <summary>
		/// Finds all instanceable script types for each bot type
		/// </summary>
		private static void distinguishTypes()
		{	//Check each class in our assembly
			Type[] types = _scripts.GetTypes();

			//Find all script interfaces
			List<Type> interfaces = new List<Type>();

			foreach (Type t in types)
			{	//Scripting class type?
				if (typeof(IScript).IsAssignableFrom(t))
					//Yep, addit!
					interfaces.Add(t);
			}

			//For every bot type..
			foreach (InvokerType type in _invokerTypes)
			{	//Start our list of script interfaces
				List<Type> scripts = new List<Type>();

				//Check all the present interfaces..
				foreach (Type inter in interfaces)
				{	//If it's just in the default namespace, it's a default
					//script, so add it to the list
					if (inter.Namespace == "InfServer")
						scripts.Add(inter);
					//Otherwise, if it's a script derivative..
					else if (inter.Namespace.StartsWith("InfServer.Script."))
					{	//What's the flavour?
						string bottype = inter.Namespace.Split('.')[2];

						//If it's our bot, use it
						if (bottype.Equals(type.name, StringComparison.CurrentCultureIgnoreCase))
							scripts.Add(inter);
					}
				}

				//We're done, add our list
				_invokerScripts[type.name] = scripts;
			}

			//Add scripts for a purely default environment
			//Start our list of script interfaces
			List<Type> defaultScripts = new List<Type>();

			//Add all default scripts
			foreach (Type inter in interfaces)
				if (inter.Namespace == "InfServer")
					defaultScripts.Add(inter);

			//We're done, add our list
			_invokerScripts[""] = defaultScripts;
		}

		/// <summary>
		/// Compiles an assembly for a specific invoker type
		/// </summary>
		private static bool compile(List<ScriptParser> parsers, string[] sources, string[] namespaces)
		{	//Obtain our compiler
			var providerOptions = new Dictionary<string, string>();
			providerOptions.Add("CompilerVersion", "v4.0");

			CodeDomProvider compiler = new CSharpCodeProvider(providerOptions);

			//Prepare the compiler settings
			CompilerParameters compilerParams = new CompilerParameters();

			//We want debug information
			compilerParams.CompilerOptions += " /d:DEBUG /d:TRACE";
			compilerParams.IncludeDebugInformation = true;

			//We want to generate a dll
			compilerParams.GenerateExecutable = false;
			compilerParams.GenerateInMemory = false;

			//Load all referenced assemblies in each script
			UniqueAssemblyLocations requestedRefAsms = new UniqueAssemblyLocations();

			//Add every assembly in our current appdomain
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					requestedRefAsms.AddAssembly(asm.Location);
				}
				catch (ArgumentException)
				{
					//Seem to sometimes encounter bogus paths for some reason
				}
				catch (NotSupportedException)
				{
					//under ASP.NET some assemblies do not have a location (e.g. dynamically built/emitted assemblies)
				}
			}

			//Resolve and add each namespace
			string[] searchDirs = getIncludeDirs().ToArray();

			foreach (string asmspace in namespaces)
			{	//Find the assemblies for the namespace
				string[] assemblies = AssemblyResolver.FindAssembly(asmspace, searchDirs);

				foreach (string asm in assemblies)
					requestedRefAsms.AddAssembly(asm);
			}

			//Cater for linq
			requestedRefAsms.AddAssembly(AssemblyResolver.FindAssembly("System.Core", new string[0])[0]);

			compilerParams.ReferencedAssemblies.AddRange((string[])requestedRefAsms);

			foreach (string sref in compilerParams.ReferencedAssemblies)
				Log.write(TLog.Inane, "\t- Ref: " + sref);

			//Determine our output assembly filename
			string compiledAssembly = System.Environment.CurrentDirectory + "\\scripts.cmp";

			if (File.Exists(compiledAssembly))
				File.Delete(compiledAssembly);

			compilerParams.OutputAssembly = compiledAssembly;

			//Attempt to compile
			CompilerResults results = compiler.CompileAssemblyFromFile(compilerParams, sources);

			//If there were errors..
			if (results.Errors.Count > 0)
			{	//Let's be so kind as to describe them
				string result = "Errors whilst compiling script code:\n";

				foreach (CompilerError err in results.Errors)
				{	//As long as it's not a warning, append the error
					if (err.IsWarning)
						continue;

					//We need to fix the script name, get the associated script
					foreach (ScriptParser parser in parsers)
					{	//Is this the file?
						if (parser.m_preparedFile.Equals(err.FileName, StringComparison.CurrentCultureIgnoreCase))
						{	//If it was temped, use the real path
							if (parser.m_bTemped)
								err.FileName = parser.m_filepath;
							break;
						}
					}

					result += err.ToString() + "\n\n";
				}

				//Throw eet
				throw new ApplicationException(result);
			}

			//We're good, grab our assembly
			_scripts = results.CompiledAssembly;

			//Done!
			Log.write("\tCompile successful!");
			return true;
		}

		/// <summary>
		/// Loads all scripts from a directory
		/// </summary>
		static private void loadDirScripts(ref List<ScriptParser> scripts, string directory)
		{	//Create lists of files to define for each bot type
			foreach (string dir in Directory.GetDirectories(directory))
			{	//For each cs file in this directory
				foreach (string f in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
				{	//Attempt to compile it
					string[] dirpath = f.Split(new char[] { Path.DirectorySeparatorChar });

					//Obtain the subdir path
					string path = null;

					for (int i = 0; i < dirpath.Length - 1; ++i)
					{	//Look for scripts..
						if (dirpath[i].Equals("scripts", StringComparison.CurrentCultureIgnoreCase))
							//Start forming our path
							path = new string(' ', 0);
						else if (path != null)
							path += dirpath[i] + "\\";
					}

					//Trim the last slash
					path = path.Trim(new char[] { '\\' });

					//Parse the script and add to the dictionary
					ScriptParser parser = new ScriptParser(f, true);

					scripts.Add(parser);

					//Which bot type does this belong to?
					InvokerType scriptType = null;
					foreach (InvokerType bt in _invokerTypes)
						if (bt.scriptDir == path)
						{
							scriptType = bt;
							break;
						}

					//If it belongs to a specific type, add it
					if (scriptType != null)
					{
						parser.m_namespace = "InfServer." + scriptType.name;
						scriptType.scripts.Add(f);
					}
				}
			}
		}

		/// <summary>
		/// Loads and compiles all the scripts it can find in the scripts directory
		/// </summary>
		static public bool compileScripts()
		{	//Look in the scripts directories for compilable cs files
			string scriptDirectory = System.Environment.CurrentDirectory + "\\scripts\\";
			List<ScriptParser> scripts = new List<ScriptParser>();

			loadDirScripts(ref scripts, scriptDirectory);

			//Create our source codes
			List<string> sources = new List<string>();
			List<string> namespaces = new List<string>();

			foreach (ScriptParser script in scripts)
			{	//Add the script code
				sources.Add(script.prepare());

				//Add all referenced namespaces
				foreach (string space in script.m_namespaces)
					namespaces.Add(space);
			}

			//Preform the compile!
			if (compile(scripts, sources.ToArray(), namespaces.ToArray()))
				//Excellent, let's sort out the types
				distinguishTypes();
			else
				return false;

			return true;
		}
		#endregion Complilation

		#region Helper functions
		/// <summary>
		/// Creates a list of directories to search for assemblies in
		/// </summary>
		static public List<string> getIncludeDirs()
		{	//Get adding!
			List<string> dirs = new List<string>();

			//Add the bot's directory
			dirs.Add(Environment.CurrentDirectory);

			return dirs;
		}
		#endregion

		/// <summary>
		/// Loads a list of bot types
		/// </summary>
		static public void loadBotTypes(List<InvokerType> types)
		{	//Store the types
			_invokerTypes = types;

			//Initialize each bot type ready for compiling
			foreach (InvokerType type in _invokerTypes)
				type.scripts = new List<string>();
		}
	}

	/// <summary>
	/// Represents a tpye of script invoker
	/// </summary>
	public class InvokerType
	{	//Variables
		public string name;					//Name of the bot type
		public bool inheritDefaultScripts;	//Does the bot load default scripts?
		public string scriptDir;			//The name of the bot's script directory

		//Internal scripting variables
		public List<string> scripts;		//The filenames of the scripts to compile for this type

		//Constructor
		public InvokerType(string a, bool b, string c)
		{
			name = a;
			inheritDefaultScripts = b;
			scriptDir = c;
		}
	}
}
