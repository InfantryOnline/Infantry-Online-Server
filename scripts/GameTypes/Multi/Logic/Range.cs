using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Security.Cryptography;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;
using InfServer.Network;
using InfServer.Data;

using Assets;

namespace InfServer.Script.GameType_Multi
{/// <summary>The Range class.</summary>
 /// <typeparam name="T">Generic parameter.</typeparam>
    public class Range
    {
       public int max = 0;
       public int min = 0;

        public Range(int lower, int upper)
        {
            min = lower;
            max = upper;
        }
    }

    public static class NumericExtentions
    {
        public static bool IsWithin(this int value, int minimum, int maximum)
        {
            return value >= minimum && value <= maximum;
        }

    }
}
