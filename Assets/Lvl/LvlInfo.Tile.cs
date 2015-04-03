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
            /// <summary>
            /// Is this tile blocked? (No physic/clear)
            /// </summary>
            public bool Blocked
			{ get { return (PhysicsVision & 0x1F) != 0; } }

            /// <summary>
            /// Gets the actual physic (Clear = 0 to Blue = 31)
            /// </summary>
			public int Physics
			{ get { return (PhysicsVision & 0x1F); } }

            /// <summary>
            /// Gets the vision tile number (0-8, 0 being clear)
            /// </summary>
            public int Vision
			{ get { return PhysicsVision >> 5; } }

			public int TerrainLookup
			{ get { return (Unknown0 & 0x7F); } }
        }
    }
}
