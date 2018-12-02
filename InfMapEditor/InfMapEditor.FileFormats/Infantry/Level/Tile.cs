/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

namespace InfMapEditor.FileFormats.Infantry.Level
{
    public struct Tile
    {
        public byte BitsA;
        public byte BitsB;
        public byte BitsC;

        public bool IsBlocked
        {
            get { return this.Physics != 0; }
        }

        public int Physics
        {
            get { return this.BitsC & 0x1F; }
            set
            {
                this.BitsC &= 0xE0; // ~0x1F
                this.BitsC |= (byte)(value & 0x1F);
            }
        }

        public int Vision
        {
            get { return this.BitsC >> 5; }
            set
            {
                this.BitsC &= 0x1F; // ~0xE0
                this.BitsC |= (byte)(value << 5);
            }
        }

        public int TerrainLookup
        {
            get { return (this.BitsA & 0x7F); }
            set
            {
                this.BitsA &= 0x80;
                this.BitsA |= (byte)(value & 0x7F);
            }
        }
    }
}
