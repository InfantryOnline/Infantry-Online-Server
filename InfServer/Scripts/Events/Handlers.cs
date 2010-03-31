using System;
using System.Collections.Generic;

using System.Threading;
using System.Reflection;
using System.Reflection.Emit;

using InfServer.Scripting;


namespace InfServer
{
	// Custom Event Classes
	/// For nice handling of custom events
	//////////////////////////////////////////////////////
	public delegate object CustomInvokeHandler(CEventHandler ehandler, bool bCall, object target, object[] parameters);
	public delegate object InvokeHandler(object target, object[] parameters);

	public class CEventHandler
	{
		public object that;					//The this pointer for the function
		public string funcname;				//The name of the function we're calling
		public InvokeHandler handler;		//The stub handler to call the function
		public CustomInvokeHandler customcaller;
		//Optional handler for calling the function
		//under customisable circumstances

		public int argnum;					//The amount of arguments we're expecting

		public string guid;					//GUID for a series of handlers

		//Constructors
		public CEventHandler(object obj, string name)
		{
			that = obj;
			funcname = name;
		}

		public CEventHandler(object obj, string name, CustomInvokeHandler custom)
		{
			that = obj;
			funcname = name;

			customcaller = custom;
		}
	}

	public class EventHandlers : Dictionary<string, HandlerList>
	{	//Variables
		public string m_seriesGUID;	//The guid of the current series
		IEventObject m_eventobject;	//The event object which owns this handler list

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		//Constructor
		public EventHandlers(IEventObject eventobject)
		{
			m_eventobject = eventobject;
		}

		// unloadSeries
		/// Unloads a series of handlers with the same guid
		///////////////////////////////////////////////////
		public void unloadSeries(string guid)
		{	//Be threadsafe
			using (DdMonitor.Lock(m_eventobject._sync))
			{	//Search through all our events
				foreach (KeyValuePair<string, HandlerList> kvp in this)
				{	//For each handler
					List<CEventHandler> toRemove = new List<CEventHandler>();

					foreach (CEventHandler eh in kvp.Value.methods)
					{	//If the handler is in this series
						if (eh.guid == guid)
							//Add it to the removal list
							toRemove.Add(eh);
					}

					//Remove all condemned handlers
					foreach (CEventHandler eh in toRemove)
						kvp.Value.methods.Remove(eh);
				}
			}
		}

		// Indexer
		/// Returns a handlerlist for the specified event
		///////////////////////////////////////////////////
		public new HandlerList this[string key]
		{
			get
			{	//If the handlerlist doesn't exist
				HandlerList list;

				if (!TryGetValue(key, out list))
				{	//Create a new one
					HandlerList newlist = new HandlerList(m_eventobject, new List<CEventHandler>());

					//Add it and return it
					base.Add(key, newlist);
					return newlist;
				}
				else
					//Otherwise just return it
					return list;
			}

			set
			{	//We don't want anyone changing our handlerlists
				//throw new InvalidOperationException("Attempted to change handlerlist.");
			}
		}
	}

	public class HandlerList
	{	//Variables
		public List<CEventHandler> methods;
		private IEventObject owner;

		//Constructor
		public HandlerList(IEventObject a, List<CEventHandler> b)
		{ owner = a; methods = b; }

		//Override the += operator
		public static HandlerList operator +(HandlerList a, CEventHandler b)
		{	//Return the new class..
			HandlerList ret = new HandlerList(a.owner, a.methods);

			//Grab the class' type
			Type objtype = b.that.GetType();
			if (objtype == null)
				throw new ArgumentException("Invalid class object given as a handler.");

			//Coax the method info out of the class
			MethodInfo mi = objtype.GetMethod(b.funcname);
			if (mi == null)
				throw new ArgumentException("Invalid function name given as a handler. (not public?)");

			//Populate the event handler class
			b.handler = getInvoker(mi);
			b.argnum = mi.GetParameters().Length;
			b.guid = (a.owner == null) ? null : a.owner.events.m_seriesGUID;

			//Add an extra handler and return
			ret.methods.Add(b);
			return ret;
		}

		// Invoker Construction Functions
		///////////////////////////////////////////////////
		static private InvokeHandler getInvoker(MethodInfo methodInfo)
		{	//Create our new stub invoker method
			DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, typeof(object), new Type[] { typeof(object), typeof(object[]) }, methodInfo.DeclaringType.Module);
			ILGenerator il = dynamicMethod.GetILGenerator();
			ParameterInfo[] ps = methodInfo.GetParameters();


			// Gather the parameter details
			///////////////////////////////////////////////
			int i = 0;
			Dictionary<LocalBuilder, Type> parameters
				= new Dictionary<LocalBuilder, Type>();

			foreach (ParameterInfo psi in ps)
			{	//Get our real parameter type
				Type realtype;

				//If it's a reference type, dereference it
				if (ps[i].ParameterType.IsByRef)
					realtype = psi.ParameterType.GetElementType();
				else
					realtype = psi.ParameterType;

				//Create a local variable builder for this parameter
				//for later usage with the stub function
				LocalBuilder local = il.DeclareLocal(realtype, true);

				//Add it to our parameter list
				parameters.Add(local, realtype);
			}


			// Piece together the stub code
			///////////////////////////////////////////////
			foreach (KeyValuePair<LocalBuilder, Type> kvp in parameters)
			{	//Load the argument given to the stub onto the stack
				il.Emit(OpCodes.Ldarg_1);
				EmitFastInt(il, i++);

				//Make a cast to the given type
				il.Emit(OpCodes.Ldelem_Ref);
				EmitCastToReference(il, kvp.Value);

				//Store the convert argument into our local value
				il.Emit(OpCodes.Stloc, kvp.Key);
			}

			//Begin loading all the arguments for a call
			//If we're not a static function we want our this pointer
			if (!methodInfo.IsStatic)
				il.Emit(OpCodes.Ldarg_0);


			//Load all our parameters onto the stack
			i = 0;

			foreach (KeyValuePair<LocalBuilder, Type> kvp in parameters)
			{	//By ref or by 
				if (ps[i++].ParameterType.IsByRef)
					il.Emit(OpCodes.Ldloca_S, kvp.Key);
				else
					il.Emit(OpCodes.Ldloc, kvp.Key);
			}

			//Perform a normal or thiscall to the method
			if (methodInfo.IsStatic)
				il.EmitCall(OpCodes.Call, methodInfo, null);
			else
				il.EmitCall(OpCodes.Callvirt, methodInfo, null);

			//If there's no return type, load a null object
			if (methodInfo.ReturnType == typeof(void))
				il.Emit(OpCodes.Ldnull);
			else
				//Otherwise convert the return type to an object
				EmitBoxIfNeeded(il, methodInfo.ReturnType);


			//If any parameters are byref, we want to
			//copy over any changes.
			i = 0;

			foreach (KeyValuePair<LocalBuilder, Type> kvp in parameters)
			{	//If it's byref
				if (ps[i].ParameterType.IsByRef)
				{	//Load the argument
					il.Emit(OpCodes.Ldarg_1);
					EmitFastInt(il, i);
					il.Emit(OpCodes.Ldloc, kvp.Key);
					if (kvp.Key.LocalType.IsValueType)
						il.Emit(OpCodes.Box, kvp.Key.LocalType);

					//And store it
					il.Emit(OpCodes.Stelem_Ref);
				}

				i++;
			}

			//Return from the stub
			il.Emit(OpCodes.Ret);

			//Return our newly created stub-delegate
			return (InvokeHandler)dynamicMethod.CreateDelegate(typeof(InvokeHandler));
		}

		private static void EmitCastToReference(ILGenerator il, System.Type type)
		{	//Cast the value/object to the given type
			if (type.IsValueType)
				il.Emit(OpCodes.Unbox_Any, type);
			else
				il.Emit(OpCodes.Castclass, type);
		}

		private static void EmitBoxIfNeeded(ILGenerator il, System.Type type)
		{	//Turn the value into an object (box it) if required
			if (type.IsValueType)
				il.Emit(OpCodes.Box, type);
		}

		private static void EmitFastInt(ILGenerator il, int value)
		{	//If there is a constant opcode for the integer..
			switch (value)
			{	//.. write it!
				case -1:
					il.Emit(OpCodes.Ldc_I4_M1);
					return;
				case 0:
					il.Emit(OpCodes.Ldc_I4_0);
					return;
				case 1:
					il.Emit(OpCodes.Ldc_I4_1);
					return;
				case 2:
					il.Emit(OpCodes.Ldc_I4_2);
					return;
				case 3:
					il.Emit(OpCodes.Ldc_I4_3);
					return;
				case 4:
					il.Emit(OpCodes.Ldc_I4_4);
					return;
				case 5:
					il.Emit(OpCodes.Ldc_I4_5);
					return;
				case 6:
					il.Emit(OpCodes.Ldc_I4_6);
					return;
				case 7:
					il.Emit(OpCodes.Ldc_I4_7);
					return;
				case 8:
					il.Emit(OpCodes.Ldc_I4_8);
					return;
			}

			//Otherwise insert the constant manually
			if (value > -129 && value < 128)
				il.Emit(OpCodes.Ldc_I4_S, (SByte)value);
			else
				il.Emit(OpCodes.Ldc_I4, value);
		}

		public object TypeConvert(object source, Type DestType)
		{	//Change the object type
			return System.Convert.ChangeType(source, DestType);
		}
	}
}