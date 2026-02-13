using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using InfServer.Protocol;

namespace InfServer.Game.Commands
{
	/// <summary>
	/// Used to register all chat and mod commands
	/// </summary>
	public class Registrar
	{	/// <summary>
		/// A list of mod command handlers indexed by command name 
		/// </summary>
		public Dictionary<string, HandlerDescriptor> _modCommands;
		
		/// <summary>
		/// A list of chat command handlers indexed by command name 
		/// </summary>
		public Dictionary<string, HandlerDescriptor> _chatCommands;

        /// <summary>
        /// A list of communication command handlers indexed by command name
        /// </summary>
        public Dictionary<string, HandlerDescriptor> _commCommands;

		/// <summary>
		/// Indicates whether we have registered our internal handlers yet
		/// </summary>
		private bool bInternalRegistered;
		
		/// <summary>
		/// Finds all registry functions in the given assembly
		/// </summary>
		public List<HandlerInfo> findRegistrars(Assembly asm)
		{	//Instance some lists
			List<HandlerInfo> regFunctions = new List<HandlerInfo>();
			_modCommands = new Dictionary<string, HandlerDescriptor>();
			_chatCommands = new Dictionary<string, HandlerDescriptor>();
            _commCommands = new Dictionary<string, HandlerDescriptor>();

			//Search for our namespace
			IEnumerable<Type> classes = asm.GetTypes().Where(
				type => (type.Namespace != null && type.Namespace.StartsWith("InfServer.Game.Commands")));

			foreach (Type type in classes)
			{
				foreach (MethodInfo method in type.GetMethods())
				{	//If it has a registryfunc attribute..
					RegistryFunc attr = 
						method.GetCustomAttributes(true).FirstOrDefault(a => a is RegistryFunc) as RegistryFunc;

					if (method.IsStatic && attr != null)
					{	//Create a new descriptive struct
						HandlerInfo handler = new HandlerInfo();

						handler.type = attr.type;
						handler.method = method;

						regFunctions.Add(handler);
					}
				}
			}

			//Got them!
			return regFunctions;
		}
		
		/// <summary>
		/// Searches for compatible functions in the logic namespace
		/// for registration
		/// </summary>
		public void register()
		{	//Do we need registering?
			if (!bInternalRegistered)
			{	//Get our internal handler functions
				List<HandlerInfo> internalRegs = findRegistrars(Assembly.GetExecutingAssembly());
				bInternalRegistered = true;

				//Register them
				registerList(internalRegs);
			}

			//Register the calling assembly's functions
			List<HandlerInfo> callerRegs = findRegistrars(Assembly.GetCallingAssembly());

			//Register them
			registerList(callerRegs);
		}

		/// <summary>
		/// Registers a list of handlers within the registrar class
		/// </summary>
		public void registerList(List<HandlerInfo> handlers)
		{
			foreach (HandlerInfo handler in handlers)
			{	//Obtain a list of appropriate handlers
				IEnumerable<HandlerDescriptor> descriptors = (IEnumerable<HandlerDescriptor>) handler.method.Invoke(null, new object[] { });

				if (descriptors == null)
				{	//Report and abort
					Log.write(TLog.Error, "Registrar method '{0}' did not return a list of handler descriptors.", handler.method);
					continue;
				}
				
				//We have 'em, add 'em
				switch (handler.type)
				{
                    case HandlerType.CommCommand:
                        {   //Add each handler
                            foreach (HandlerDescriptor h in descriptors)
                                _commCommands.Add(h.handlerCommand.ToLower(), h);
                        }
                        break;

					case HandlerType.ChatCommand:
						{	//Add each handler
							foreach (HandlerDescriptor h in descriptors)
								_chatCommands.Add(h.handlerCommand.ToLower(), h);
						}
						break;

					case HandlerType.ModCommand:
						{	//Add each handler
							foreach (HandlerDescriptor h in descriptors)
								_modCommands.Add(h.handlerCommand.ToLower(), h);
						}
						break;
				}
			}
		}
	}

	/// <summary>
	/// Distinguishes between types of handlers
	/// </summary>
	public enum HandlerType
	{
		ModCommand,
		ChatCommand,
        CommCommand,
	}

	/// <summary>
	/// Distinguishes between types of handlers
	/// </summary>
	public struct HandlerInfo
	{
		public HandlerType type;
		public MethodInfo method;
	}

	[Flags]
	public enum PermissionAuthority
	{
		None	= 0,
		Player  = 1,
		Host	= 2,
		Mod		= 4,
		ZoneMod = 8
	}

	public class HandlerPermissionRequirement
	{
		public PermissionAuthority Authority { get; set; }

		public Data.PlayerPermission PermissionLevel { get; set; }
	}

	/// <summary>
	/// Describes a specific chat or mod command handler
	/// </summary>
	public struct HandlerDescriptor
	{
		public Action<Player, Player, string, int> handler;

		public string handlerCommand;
		public string commandDescription;
		public string usage;

		public HandlerPermissionRequirement Permission { get; set; }

		/// <summary>
		/// Backwards compatibility with scripts. Don't use.
		/// </summary>
		[Obsolete("Backwards compatibility with scripts. Don't use")]
		public Data.PlayerPermission permissionLevel
		{
			get { return Permission.PermissionLevel; }
			set { Permission.PermissionLevel = value; }
		}

        /// <summary>
        /// Backwards compatibility with scripts. Don't use.
        /// </summary>
        [Obsolete("Backwards compatibility with scripts. Don't use")]
        public bool isDevCommand
		{
			get { return Permission.Authority.HasFlag(PermissionAuthority.Host); }
			set
			{
				if (value) Permission.Authority |= PermissionAuthority.Host;
				else Permission.Authority &= ~PermissionAuthority.Host;
			}
		}

		public HandlerDescriptor(Action<Player, Player, string, int> _handler, string _handlerCommand, string _commandDescription, string _usage)
		{
			handler = _handler;
			handlerCommand = _handlerCommand;
			commandDescription = _commandDescription;
			usage = _usage;

            Permission = new HandlerPermissionRequirement
			{
				PermissionLevel = Data.PlayerPermission.Normal,
				Authority = PermissionAuthority.Player
			};

            isDevCommand = false;
		}

		public HandlerDescriptor(Action<Player, Player, string, int> _handler, string _handlerCommand, string _commandDescription, string _usage, Data.PlayerPermission _permissionLevel, bool _isDevCommand)
		{
			handler = _handler;
			handlerCommand = _handlerCommand;
			commandDescription = _commandDescription;
			usage = _usage;

            Permission = new HandlerPermissionRequirement
            {
                PermissionLevel = _permissionLevel,
                Authority = PermissionAuthority.Mod | PermissionAuthority.ZoneMod 
            };

			if (_isDevCommand)
			{
				Permission.Authority |= PermissionAuthority.Host;
			}
		}

		public HandlerDescriptor(Action<Player, Player, string, int> _handler, string _handlerCommand, string _commandDescription, string _usage, Data.PlayerPermission _permissionLevel, PermissionAuthority authority)
		{
            handler = _handler;
            handlerCommand = _handlerCommand;
            commandDescription = _commandDescription;
            usage = _usage;
			Permission = new HandlerPermissionRequirement { Authority = authority, PermissionLevel = _permissionLevel };
        }
	}

	/// <summary>
	/// Used to allow preregistration of handlers et al
	/// </summary>
	public class RegistryFunc : Attribute
	{
		public HandlerType type;

		public RegistryFunc(HandlerType _type)
		{
			type = _type;
		}
	}
}
