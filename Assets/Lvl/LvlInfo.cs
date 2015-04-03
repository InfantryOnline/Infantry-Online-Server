using System;
using System.IO;
using System.Collections.Generic;

namespace Assets
{
    public partial class LvlInfo
    {
        public int Width;
        public int Height;
        public BlobReference[] ObjectBlobs;
        public BlobReference[] FloorBlobs;
        public int[] TerrainLookup = new int[128];
        public uint LightColorWhite;
        public uint LightColorRed;
        public uint LightColorGreen;
        public uint LightColorBlue;

        public short[] PhysicsLow;
        public short[] PhysicsHigh;

        public Tile[] Tiles;
        public int OffsetX;
        public int OffsetY;

        //Examples:
        //  ___
        //  | / = Red Upper Left
        //  |/
        //
        //   /| = Red Lower Right
        //  / |
        // ----
        private readonly List<string> physicValues = new List<string> {
            "Clear",
            "Red Solid","Red Upper Left","Red Upper Right","Red Lower Left","Red Lower Right",
            "Green Solid","Green Upper Left","Green Upper Right","Green Lower Left","Green Lower Right",
            "Yellow Solid","Yellow Upper Left","Yellow Upper Right","Yellow Lower Left","Yellow Lower Right",
            "Orange Solid","Orange Upper Left","Orange Upper Right","Orange Lower Left","Orange Lower Right",
            "Purple Solid","Purple Upper Left","Purple Upper Right","Purple Lower Left","Purple Lower Right",
            "Red Move Right", "Red Move Left", "Red Move Down", "Red Move Up",
            "Teal Solid",
            "Blue Solid"
        };

        /// <summary>
        /// Returns the physic color value at postion(x, y) in the level.
        /// </summary>
        /// <returns>The physic color at level position (x, y)</returns>
        public string getPhysicColorAt(int x, int y)
        {
            if (x / 16 >= Width || x < 0)
                throw new ArgumentOutOfRangeException("Argument x is out of range");
            if (y / 16 >= Height || x < 0)
                throw new ArgumentOutOfRangeException("Argument y is out of range");
            x /= 16;
            y /= 16;
            return physicValues[Tiles[y * Width + x].PhysicsVision & 0x1F];
        }

        /// <summary>
        /// Returns the tile at position (x, y) in the level.
        /// </summary>
        /// <param name="x">x-coordinate of the tile's location</param>
        /// <param name="y">y-coordinate of the tile's location</param>
        /// <returns>The tile object at position (x, y)</returns>
        public Tile getTileAt(int x, int y)
        {
            if(x / 16 >= Width || x < 0)
                throw new ArgumentOutOfRangeException("Argument x is out of range");
            if(y / 16 >= Height || y < 0)
                throw new ArgumentOutOfRangeException("Argument y is out of range");
            x /= 16;
            y /= 16;
            return Tiles[y*Width + x];
        }

        /// <summary>
        /// Returns true if the tile has a physic there (Not clear/color based).
        /// </summary>
        public bool isTileBlocked(int x, int y)
        {
            if (x / 16 >= Width || x < 0)
                throw new ArgumentOutOfRangeException("Argument x is out of range");
            if (y / 16 >= Height || y < 0)
                throw new ArgumentOutOfRangeException("Argument y is out of range");
            x /= 16;
            y /= 16;
            return Tiles[y*Width + x].Blocked;
        }

        /// <summary>
        /// Returns true if vehicle cannot traverse over the tile located at (x, y).
        /// A tile is considered to be blocked if it's low and high physics encompass the vehicles.
        /// </summary>
        /// <param name="x">x-coordinate of the tile</param>
        /// <param name="y">y-coordinate of the tile</param>
        /// <returns>True if tile is impassable</returns>
        public bool isTileBlocked(int x, int y, VehInfo vehicle)
        {
            if (vehicle == null)
                return false;
            bool blocked = isTileBlocked(x, y);
            //Is this vehicle low enough to run under?
            if (blocked)
            {
                x /= 16;
                y /= 16;
                int phy = Tiles[y * Width + x].Physics;
                short low = PhysicsLow[phy];
                short high = PhysicsHigh[phy];
                if (vehicle.LowZ <= low || vehicle.HighZ >= high)
                    blocked = !blocked;
            }
            return blocked;
        }

        /// <summary>
        /// Returns the vision constant for this tile.
        /// </summary>
        /// <param name="x">x-coordinate of the tile's location</param>
        /// <param name="y">y-coordinate of the tile's location</param>
        /// <returns>The vision constant at this tile.</returns>
        public int getTileVision(int x, int y)
        {
            if (x / 16 >= Width || x < 0)
                throw new ArgumentOutOfRangeException("Argument x is out of range");
            if (y / 16 >= Height || y < 0)
                throw new ArgumentOutOfRangeException("Argument y is out of range");
            x /= 16;
            y /= 16;
            return Tiles[y*Width + x].Vision;
        }

        public override string ToString()
        {
            return String.Format("[LvlInfo Width({0}), Height({1})]", Width, Height);
        }

        public string ToString(int x, int y)
        {
            if (x / 16 >= Width || x < 0)
                throw new ArgumentOutOfRangeException("Argument x is out of range");
            if (y / 16 >= Height || y < 0)
                throw new ArgumentOutOfRangeException("Argument y is out of range");
            x /= 16;
            y /= 16;
            int phy = Tiles[y * Width + x].Physics;
            short low = PhysicsLow[phy];
            short high = PhysicsHigh[phy];
            return this + String.Format("[Tile({0},{1}) PhysLow({2}), PhysHigh({3})]", x, y, low, high);
        }

        public static LvlInfo Load(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            Stream input = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            LvlInfo lvlInfo = new LvlInfo();

            Header header = input.ReadStructure<Header>();

            lvlInfo.OffsetX = header.OffsetX;
            lvlInfo.OffsetY = header.OffsetY;

            #region Version < 4
            if (header.Version < 4)
            {
                for (int i = 0; i < header.PhysicsLow.Length; i++)
                {
                    header.PhysicsLow[i] = 0;
                }

                header.LightColorWhite = 0xFFFFFF00u;
                header.LightColorRed = 0x0000FF00u;
                header.LightColorGreen = 0x00FF0000u;
                header.LightColorBlue = 0xFF000000u;

                header.PhysicsHigh[0] = 0;
                header.PhysicsHigh[1] = 1024;
                header.PhysicsHigh[2] = 1024;
                header.PhysicsHigh[3] = 1024;
                header.PhysicsHigh[4] = 1024;
                header.PhysicsHigh[5] = 1024;
                header.PhysicsHigh[6] = 16;
                header.PhysicsHigh[7] = 16;
                header.PhysicsHigh[8] = 16;
                header.PhysicsHigh[9] = 16;
                header.PhysicsHigh[10] = 16;
                header.PhysicsHigh[11] = 32;
                header.PhysicsHigh[12] = 32;
                header.PhysicsHigh[13] = 32;
                header.PhysicsHigh[14] = 32;
                header.PhysicsHigh[15] = 32;
                header.PhysicsHigh[16] = 64;
                header.PhysicsHigh[17] = 64;
                header.PhysicsHigh[18] = 64;
                header.PhysicsHigh[19] = 64;
                header.PhysicsHigh[20] = 64;
                header.PhysicsHigh[21] = 128;
                header.PhysicsHigh[22] = 128;
                header.PhysicsHigh[23] = 128;
                header.PhysicsHigh[24] = 128;
                header.PhysicsHigh[25] = 128;
                header.PhysicsHigh[26] = 1024;
                header.PhysicsHigh[27] = 1024;
                header.PhysicsHigh[29] = 1024;
                header.PhysicsHigh[28] = 1024;
                header.PhysicsHigh[30] = 1024;
                header.PhysicsHigh[31] = 1024;
            }
            #endregion

            for (int i = 0; i < header.TerrainLookup.Length; i++)
            {
                header.TerrainLookup[i] %= 16;
            }

            #region Blobs
            if (header.Version >= 6)
            {
                lvlInfo.ObjectBlobs = new BlobReference[header.ObjectCount];
                for (int i = 0; i < lvlInfo.ObjectBlobs.Length; i++)
                {
                    lvlInfo.ObjectBlobs[i] = input.ReadStructure<BlobReference>();
                }

                lvlInfo.FloorBlobs = new BlobReference[header.FloorCount];
                for (int i = 0; i < lvlInfo.FloorBlobs.Length; i++)
                {
                    lvlInfo.FloorBlobs[i] = input.ReadStructure<BlobReference>();
                }
            }
            else
            {
                string lvb = Path.ChangeExtension(Path.GetFileName(path), ".lvb");

                lvlInfo.ObjectBlobs = new BlobReference[header.ObjectCount];
                for (int i = 0; i < lvlInfo.ObjectBlobs.Length; i++)
                {
                    lvlInfo.ObjectBlobs[i] = new BlobReference();
                    lvlInfo.ObjectBlobs[i].FileName = lvb;
                    lvlInfo.ObjectBlobs[i].Id = string.Format("o{0}.cfs", i);
                }

                lvlInfo.FloorBlobs = new BlobReference[header.FloorCount];
                for (int i = 0; i < lvlInfo.FloorBlobs.Length; i++)
                {
                    lvlInfo.FloorBlobs[i] = new BlobReference();
                    lvlInfo.FloorBlobs[i].FileName = lvb;
                    lvlInfo.FloorBlobs[i].Id = string.Format("f{0}.cfs", i);
                }
            }
            #endregion

            lvlInfo.Width = header.Width;
            lvlInfo.Height = header.Height;
			lvlInfo.TerrainLookup = (int[])header.TerrainLookup.Clone();
            lvlInfo.PhysicsLow = (short[])header.PhysicsLow.Clone();
            lvlInfo.PhysicsHigh = (short[])header.PhysicsHigh.Clone();
            lvlInfo.Tiles = new Tile[header.Width * header.Height];
			
            byte[] data;

            // Stage 1
            data = input.DecompressRLE(lvlInfo.Tiles.Length);
            for (int i = 0; i < lvlInfo.Tiles.Length; i++)
            {
                lvlInfo.Tiles[i].Unknown0 = data[i];
            }

            // Stage 2
            data = input.DecompressRLE(lvlInfo.Tiles.Length);
            for (int i = 0; i < lvlInfo.Tiles.Length; i++)
            {
				lvlInfo.Tiles[i].Unknown1 = data[i];
            }

            // Stage 3
            data = input.DecompressRLE(lvlInfo.Tiles.Length);
            for (int i = 0; i < lvlInfo.Tiles.Length; i++)
            {
                lvlInfo.Tiles[i].PhysicsVision = data[i];
            }

            if (header.Version < 4)
            {
                int[] magic = new int[]
                {
                    0, 1, 2, 3, 4, 5, 26, 27,
                    28, 29, 0, 0, 0, 0, 0, 0,
                    0, 6, 7, 8, 9, 10, 26, 27,
                    28, 29, 0, 0, 6778732, 0, 0, 0,
                };

                for (int i = 0; i < lvlInfo.Tiles.Length; i++)
                {
                    byte a = lvlInfo.Tiles[i].PhysicsVision;
                    byte b = (byte)((magic[a & 0x1F] ^ a) & 0x1F);
					lvlInfo.Tiles[i].PhysicsVision ^= b;
                }
            }

            // Tile data is followed with entity (object) information.
            // Don't think that's necessary for physics/etc?
			
			input.Close();
			
            return lvlInfo;
        }
    }
}
