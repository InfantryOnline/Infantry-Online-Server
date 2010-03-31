namespace Assets
{
    public partial class LvlInfo
    {
        public struct Tile
        {	// Member variables
			////////////////////////////////
            public byte Unknown0;
            public byte Unknown1;
            public byte PhysicsVision;

			// Properties
			////////////////////////////////
            public bool Blocked
			{ get { return (PhysicsVision & 0x1F) != 0; } }

			public int Physics
			{ get { return (PhysicsVision & 0x1F); } }

            public int Vision
			{ get { return PhysicsVision >> 5; } }

			public int TerrainLookup
			{ get { return (Unknown0 & 0x7F); } }
        }
    }
}
