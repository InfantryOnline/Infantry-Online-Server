using System;
using System.IO;

namespace Parsers
{
    public partial class Level
    {
        public int Width;
        public int Height;
        public BlobReference[] ObjectBlobs;
        public BlobReference[] FloorBlobs;
        public short[] PhysicsLow;
        public short[] PhysicsHigh;
        public Tile[] Tiles;

        public static Level Load(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            Stream input = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Level level = new Level();

            Header header = input.ReadStructure<Header>();

            #region Version < 4
            if (header.Version < 4)
            {
                for (int i = 0; i < header.PhysicsLow.Length; i++)
                {
                    header.PhysicsLow[i] = 0;
                }

                header.Unknown0AA0 = 0xFFFFFF00;
                header.Unknown0AA4 = 0x0000FF00;
                header.Unknown0AA8 = 0x00FF0000;
                header.Unknown0AAC = 0xFF000000;

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

            for (int i = 0; i < header.Unknown0820.Length; i++)
            {
                header.Unknown0820[i] %= 16;
            }

            #region Blobs
            if (header.Version >= 6)
            {
                level.ObjectBlobs = new BlobReference[header.ObjectCount];
                for (int i = 0; i < level.ObjectBlobs.Length; i++)
                {
                    level.ObjectBlobs[i] = input.ReadStructure<BlobReference>();
                }

                level.FloorBlobs = new BlobReference[header.FloorCount];
                for (int i = 0; i < level.FloorBlobs.Length; i++)
                {
                    level.FloorBlobs[i] = input.ReadStructure<BlobReference>();
                }
            }
            else
            {
                string lvb = Path.ChangeExtension(Path.GetFileName(path), ".lvb");

                level.ObjectBlobs = new BlobReference[header.ObjectCount];
                for (int i = 0; i < level.ObjectBlobs.Length; i++)
                {
                    level.ObjectBlobs[i] = new BlobReference();
                    level.ObjectBlobs[i].FileName = lvb;
                    level.ObjectBlobs[i].Id = string.Format("o{0}.cfs", i);
                }

                level.FloorBlobs = new BlobReference[header.FloorCount];
                for (int i = 0; i < level.FloorBlobs.Length; i++)
                {
                    level.FloorBlobs[i] = new BlobReference();
                    level.FloorBlobs[i].FileName = lvb;
                    level.FloorBlobs[i].Id = string.Format("f{0}.cfs", i);
                }
            }
            #endregion

            level.Width = header.Width;
            level.Height = header.Height;
            level.PhysicsLow = (short[])header.PhysicsLow.Clone();
            level.PhysicsHigh = (short[])header.PhysicsHigh.Clone();
            level.Tiles = new Tile[header.Width * header.Height];

            byte[] data;

            // Stage 1
            data = input.DecompressRLE(level.Tiles.Length);
            for (int i = 0; i < level.Tiles.Length; i++)
            {
                level.Tiles[i].Unknown0 = data[i];
            }

            // Stage 2
            data = input.DecompressRLE(level.Tiles.Length);
            for (int i = 0; i < level.Tiles.Length; i++)
            {
                level.Tiles[i].Unknown1 = data[i];
            }

            // Stage 3
            data = input.DecompressRLE(level.Tiles.Length);
            for (int i = 0; i < level.Tiles.Length; i++)
            {
                level.Tiles[i].Unknown2 = data[i];
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

                for (int i = 0; i < level.Tiles.Length; i++)
                {
                    byte a = level.Tiles[i].Unknown2;
                    byte b = (byte)((magic[a & 0x1F] ^ a) & 0x1F);
                    level.Tiles[i].Unknown2 ^= b;
                }
            }

            // Tile data is followed with entity (object) information.
            // Don't think that's necessary for physics/etc?
			
			input.Close();
			
            return level;
        }
    }
}
