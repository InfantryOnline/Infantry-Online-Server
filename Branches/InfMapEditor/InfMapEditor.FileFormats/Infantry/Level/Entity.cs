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

using System.Runtime.InteropServices;

namespace InfMapEditor.FileFormats.Infantry.Level
{
    [StructLayout(LayoutKind.Sequential, Pack = 1 /*, Size = 17*/)]
    public struct Entity
    {
        public short X;
        public short Y;
        public uint BitsA;
        public uint BitsB;
        public uint BitsC;
        public byte BitsD;

        public int ObjectId
        {
            get { return (int)(this.BitsA & 0x00001FFF); }
            set
            {
                this.BitsA &= ~0x00001FFFu;
                this.BitsA |= (uint)(value & 0x00001FFF);
            }
        }

        // ReSharper disable InconsistentNaming
        public int UnknownA_1
            // ReSharper restore InconsistentNaming
        {
            get { return (int)((this.BitsA & 0x000FE000) >> 13); }
            set
            {
                this.BitsA &= ~0x000FE000u;
                this.BitsA |= (uint)((value & 0x000000007F) << 13);
            }
        }

        public int AnimationTime
        {
            get { return (int)((this.BitsA & 0x3FF00000) >> 20); }
            set
            {
                this.BitsA &= ~0x3FF00000u;
                this.BitsA |= (uint)((value & 0x000003FF) << 20);
            }
        }

        public Transluency Transluency
        {
            get { return (Transluency)((this.BitsA & 0xC0000000) >> 30); }
            set
            {
                this.BitsA &= ~0xC0000000u;
                this.BitsA |= (uint)(((int)value & 0x00000003) << 30);
            }
        }

        public int TimeDelta
        {
            get { return (int)(this.BitsB & 0x00001FFF); }
            set
            {
                this.BitsB &= ~0x00001FFFu;
                this.BitsB |= (uint)(value & 0x00001FFF);
            }
        }

        public int ExtraData
        {
            get { return (int)((this.BitsB & 0x03FFE000) >> 13); }
            set
            {
                this.BitsB &= ~0x03FFE000u;
                this.BitsB |= (uint)((value & 0x00001FFF) << 13);
            }
        }

        public DrawOrder DrawOrder
        {
            get { return (DrawOrder)((this.BitsB & 0x1C000000) >> 26); }
            set
            {
                this.BitsB &= ~0x1C000000u;
                this.BitsB |= (uint)(((int)value & 0x00000007) << 26);
            }
        }

        public DetailLevel DetailLevel
        {
            get { return (DetailLevel)((this.BitsB & 0x60000000) >> 29); }
            set
            {
                this.BitsB &= ~0x60000000u;
                this.BitsB |= (uint)(((int)value & 0x00000003) << 29);
            }
        }

        public bool SmartTransluency
        {
            get { return (this.BitsB & 0x80000000) != 0; }
            set
            {
                if (value == true)
                {
                    this.BitsB |= 0x80000000u;
                }
                else
                {
                    this.BitsB &= ~0x80000000u;
                }
            }
        }

        public int Value
        {
            get { return (sbyte)(this.BitsC & 0x000000FF); }
            set
            {
                this.BitsC &= ~0x000000FFu;
                // ReSharper disable RedundantCast
                this.BitsC |= (uint)((byte)value & 0x000000FFu);
                // ReSharper restore RedundantCast
            }
        }

        public int Saturation
        {
            get { return (sbyte)((this.BitsC & 0x0000FF00) >> 8); }
            set
            {
                this.BitsC &= ~0x0000FF00u;
                this.BitsC |= (uint)(((byte)value & 0x000000FF) << 8);
            }
        }

        public int Hue
        {
            get { return (int)((this.BitsC & 0x007F0000) >> 16); }
            set
            {
                this.BitsC &= ~0x007F0000u;
                this.BitsC |= (uint)((value & 0x0000007F) << 16);
            }
        }

        public LightColor LightColor
        {
            get { return (LightColor)(this.LightValue / 24); }
            set
            {
                var intensity = this.LightValue % 24;
                this.LightValue = ((int)value * 24) + intensity;
            }
        }

        public int LightIntensity
        {
            get { return this.LightValue % 24; }
            set
            {
                var color = this.LightValue / 24;
                this.LightValue = (color * 24) + (value % 24);
            }
        }

        private int LightValue
        {
            get { return (int)((this.BitsC & 0x3F800000) >> 23); }
            set
            {
                this.BitsC &= ~0x3F800000u;
                this.BitsC |= (uint)((value & 0x0000007F) << 23);
            }
        }

        public ExtraDataMeaning ExtraDataMeaning
        {
            get { return (ExtraDataMeaning)((this.BitsC & 0x40000000) >> 30); }
            set
            {
                this.BitsC &= ~0x40000000u;
                this.BitsC |= (uint)(((int)value & 0x00000001) << 30);
            }
        }

        // probably unused
        // ReSharper disable InconsistentNaming
        public bool UnknownC_6
            // ReSharper restore InconsistentNaming
        {
            get { return (this.BitsC & 0x80000000) != 0; }
            set
            {
                if (value == true)
                {
                    this.BitsB |= 0x80000000u;
                }
                else
                {
                    this.BitsB &= ~0x80000000u;
                }
            }
        }

        public int PaletteShift
        {
            // ReSharper disable RedundantCast
            get { return (int)(this.BitsD & 0x3F); }
            // ReSharper restore RedundantCast
            set
            {
                this.BitsC &= ~0x3Fu;
                this.BitsC |= (byte)(value & 0x3F);
            }
        }

        // probably unused
        // ReSharper disable InconsistentNaming
        public int UnknownD_1
            // ReSharper restore InconsistentNaming
        {
            // ReSharper disable RedundantCast
            get { return (int)((this.BitsD & 0xC0) >> 6); }
            // ReSharper restore RedundantCast
            set
            {
                this.BitsD &= 0x3F; //~0xC0
                this.BitsD |= (byte)((value & 0x03) << 6);
            }
        }
    }
}
