using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace InfServer.DirectoryServer.Directory
{// Registrar Class
    /// Used to allow game logic classes to register handlers
    /// and other mechanisms.
    ///////////////////////////////////////////////////////
    public class Registrar
    {	/// <summary>
        /// Indicates whether we have registered our internal handlers yet
        /// </summary>
        static private bool bInternalRegistered;

        /// <summary>
        /// Finds all registry functions in the given assembly
        /// </summary>
        static public List<MethodInfo> findRegistrars(Assembly asm)
        {	//Search for our namespace
            List<MethodInfo> regFunctions = new List<MethodInfo>();
            IEnumerable<Type> classes = asm.GetTypes().Where(
                type => (type.Namespace != null && type.Namespace.StartsWith("DirectoryServer.Directory.Logic")));

            foreach (Type type in classes)
            {
                foreach (MethodInfo method in type.GetMethods())
                {	//If it has a registryfunc attribute..
                    if (method.IsStatic && method.GetCustomAttributes(true).Any(attr => attr is RegistryFunc))
                        regFunctions.Add(method);
                }
            }

            //Got them!
            return regFunctions;
        }

        /// <summary>
        /// Searches for compatible functions in the logic namespace
        /// for registration
        /// </summary>
        static public void register()
        {	//Do we need registering?
            if (!bInternalRegistered)
            {	//Get our internal handler functions
                List<MethodInfo> internalRegs = findRegistrars(Assembly.GetExecutingAssembly());
                bInternalRegistered = true;

                //Call them all!
                foreach (MethodInfo method in internalRegs)
                    method.Invoke(null, new object[] { });
            }

            //Register the calling assembly's functions
            List<MethodInfo> callerRegs = findRegistrars(Assembly.GetCallingAssembly());

            //Call them all!
            foreach (MethodInfo method in callerRegs)
                method.Invoke(null, new object[] { });
        }
    }

    // Registry Attribute
    /// Used to allow preregistration of handlers et al
    ///////////////////////////////////////////////////////
    public class RegistryFunc : Attribute
    { }
}
