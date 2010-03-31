namespace Assets
{
    public class SpeedValues // 004216A0
    {
        public int RollAllowed; // 00
        public int RollFriction; // 04
        public int RollThrust; // 08
        public int RollRotate; // 0C
        public short RollTopSpeed; // 24
        public int StrafeThrust; //10
        public int HyperTopSpeed; // 14
        public int HyperThrust; // 18
        public int BackwardThrust; // 1C
        public int BackwardHyperThrust; // 20

        public void Parse(int version, ICsvParser parser)
        {
            this.RollAllowed = (version >= 16) ? parser.GetInt() : 0;
            this.RollFriction = (version >= 16) ? parser.GetInt() : 0;
            this.RollThrust = (version >= 32) ? parser.GetInt() : 0;
            this.RollRotate = (version >= 32) ? parser.GetInt() : 0;
            this.RollTopSpeed = parser.GetShort();
            this.StrafeThrust = parser.GetInt();
            this.HyperTopSpeed = parser.GetInt();
            this.HyperThrust = parser.GetInt();
            this.BackwardThrust = parser.GetInt();
            this.BackwardHyperThrust = (version >= 4) ? parser.GetInt() : 0;
        }
    }
}
