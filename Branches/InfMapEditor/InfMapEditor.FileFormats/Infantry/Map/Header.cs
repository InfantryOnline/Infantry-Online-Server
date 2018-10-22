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

namespace InfMapEditor.FileFormats.Infantry.Map
{
    [StructLayout(LayoutKind.Sequential, Pack = 1 /*, Size = 4168*/)]
    public struct Header
    {
        public int Version;

        public int Width;
        public int Height;

        public int OffsetX;
        public int OffsetY;

        public int EntityCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public short[] PhysicsLow;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public short[] PhysicsHigh;

        public uint LightColorWhite;
        public uint LightColorRed;
        public uint LightColorGreen;
        public uint LightColorBlue;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4000)]
        public byte[] Padding;
    }
}
