namespace Parsers
{
    public abstract partial class Vehicle : ICsvFormat
    {
        /// <summary>
        /// Class inside Car type.
        /// </summary>
        public class SpeedValues // 004216A0
        {
            public int RollAllowed { get; set; } // 00
            public int RollFriction { get; set; } // 04
            public int RollThrust { get; set; } // 08
            public int RollRotate { get; set; } // 0C
            public short RollTopSpeed { get; set; } // 24
            public int StrafeThrust { get; set; } //10
            public int HyperTopSpeed { get; set; } // 14
            public int HyperThrust { get; set; } // 18
            public int BackwardThrust { get; set; } // 1C
            public int BackwardHyperThrust { get; set; } // 20

            public void Parse(int version, ICsvReader reader)
            {
                /* NOTE: Speaking with Mongoose, he double-checked the order of parsing and it does not match up with Rick's.
                 *       Regular speeds come first, then StrafeThrust, and then Hyper speeds. See the "out.png" screenshot
                 *       under Docs/Veh branch.
                 *
                this.RollTopSpeed = reader.GetShort();
                this.StrafeThrust = reader.GetInt();
                this.HyperTopSpeed = reader.GetInt();
                this.HyperThrust = reader.GetInt();
                this.BackwardThrust = reader.GetInt();
                this.BackwardHyperThrust = (version >= 4) ? reader.GetInt() : 0;
                this.RollAllowed = (version >= 16) ? reader.GetInt() : 0;
                this.RollFriction = (version >= 16) ? reader.GetInt() : 0;
                this.RollThrust = (version >= 32) ? reader.GetInt() : 0;
                this.RollRotate = (version >= 32) ? reader.GetInt() : 0;
                 */
                this.RollAllowed = (version >= 16) ? reader.GetInt() : 0;
                this.RollFriction = (version >= 16) ? reader.GetInt() : 0;
                this.RollThrust = (version >= 32) ? reader.GetInt() : 0;
                this.RollRotate = (version >= 32) ? reader.GetInt() : 0;
                this.RollTopSpeed = reader.GetShort();
                this.StrafeThrust = reader.GetInt();
                this.HyperTopSpeed = reader.GetInt();
                this.HyperThrust = reader.GetInt();
                this.BackwardThrust = reader.GetInt();
                this.BackwardHyperThrust = (version >= 4) ? reader.GetInt() : 0;
            }
        }
    }
}
