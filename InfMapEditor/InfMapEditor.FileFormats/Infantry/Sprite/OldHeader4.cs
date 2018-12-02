﻿/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
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

namespace InfMapEditor.FileFormats.Infantry.Sprite
{
    [StructLayout(LayoutKind.Sequential, Pack = 1/*, Size = 127*/)]
    public struct OldHeader4
    {
        public ushort FrameCount;
        public ushort AnimationTime;
        public ushort Width;
        public ushort Height;
        public ushort RowCount;
        public ushort ColumnCount;
        public ushort LightCount;
        public ushort ShadowCount;
        public ushort UserDataSize;
        public byte CompressionFlags;
        public byte MaxSolidIndex;
        public uint DataSize;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string Category;

        public byte BlitMode;
        public byte RowMeaning;
        public ushort YSortAdjust;
        public byte SortTransform;
        public byte UserPaletteStart;
        public byte UserPalette;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 48)]
        public string Description;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] Unknown5F;
    }
}
