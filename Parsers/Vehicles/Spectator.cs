using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parsers
{
    public abstract partial class Vehicle : ICsvFormat
    {
        public sealed class Spectator : Vehicle
        {
        }
    }
}
