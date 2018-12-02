/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
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

using System;
using System.IO;
using Gibbed.Helpers;
using InfMapEditor.FileFormats.Infantry.Sprite;

namespace InfMapEditor.FileFormats.Infantry
{
    public class SpriteFile
    {
        public string Category;
        public string Description;

        public ushort AnimationTime;
        public ushort Width;
        public ushort Height;
        public ushort RowCount;
        public ushort ColumnCount;
        public ushort ShadowCount;
        public ushort LightCount;
        public ushort YSortAdjust;
        public ushort BlitMode;
        public ushort RowMeaning;
        public ushort Unknown20;
        public byte MaxSolidIndex;
        public byte SortTransform;
        public byte UserPaletteStart;
        public byte UserPalette;
        public CompressionFlags CompressionFlags;

        public uint[] Palette = new uint[256];
        public byte[] UserData;

        public Frame[] Frames;

        public void Deserialize(Stream input, string fileName)
        {
            var version = input.ReadValueU16();
            if (version < 2 || version > 5)
            {
                throw new FormatException(String.Format("unsupported cfs version in {0}", fileName));
            }

            var header = new Header();
            
            if (version >= 5)
            {
                header = input.ReadStructure<Header>();
            }
            else if (version >= 4)
            {
                var oldHeader = input.ReadStructure<OldHeader4>();
                header.FrameCount = oldHeader.FrameCount;
                header.AnimationTime = oldHeader.AnimationTime;
                header.Width = oldHeader.Width;
                header.Height = oldHeader.Height;
                header.RowCount = oldHeader.RowCount;
                header.ColumnCount = oldHeader.ColumnCount;
                header.ShadowCount = oldHeader.ShadowCount;
                header.LightCount = oldHeader.LightCount;
                header.UserDataSize = oldHeader.UserDataSize;
                header.CompressionFlags = (CompressionFlags)oldHeader.CompressionFlags;
                header.MaxSolidIndex = oldHeader.MaxSolidIndex;
                header.DataSize = oldHeader.DataSize;
                header.Category = oldHeader.Category;
                header.BlitMode = oldHeader.BlitMode;
                header.RowMeaning = oldHeader.RowMeaning;
                header.YSortAdjust = oldHeader.YSortAdjust;
                header.SortTransform = oldHeader.SortTransform;
                header.UserPaletteStart = oldHeader.UserPaletteStart;
                header.UserPalette = oldHeader.UserPalette;
                header.Description = oldHeader.Description;
            }
            else if (version >= 3)
            {
                var oldHeader = input.ReadStructure<OldHeader3>();
                header.FrameCount = oldHeader.FrameCount;
                header.AnimationTime = oldHeader.AnimationTime;
                header.Width = oldHeader.Width;
                header.Height = oldHeader.Height;
                header.RowCount = oldHeader.RowCount;
                header.ColumnCount = oldHeader.ColumnCount;
                header.ShadowCount = oldHeader.ShadowCount;
                header.LightCount = oldHeader.LightCount;
                header.UserDataSize = oldHeader.UserDataSize;
                header.CompressionFlags = (CompressionFlags)oldHeader.CompressionFlags;
                header.MaxSolidIndex = oldHeader.MaxSolidIndex;
                header.DataSize = oldHeader.DataSize;
                header.Category = oldHeader.Category;
                header.BlitMode = oldHeader.BlitMode;
                header.RowMeaning = oldHeader.RowMeaning;
                header.YSortAdjust = oldHeader.YSortAdjust;
                header.SortTransform = oldHeader.SortTransform;
                header.UserPaletteStart = oldHeader.UserPaletteStart;
                header.UserPalette = oldHeader.UserPalette;
            }
            else if (version >= 1)
            {
                var oldHeader = input.ReadStructure<OldHeader2>();
                header.FrameCount = oldHeader.FrameCount;
                header.AnimationTime = oldHeader.AnimationTime;
                header.Width = oldHeader.Width;
                header.Height = oldHeader.Height;
                header.RowCount = oldHeader.RowCount;
                header.ColumnCount = oldHeader.ColumnCount;
                header.ShadowCount = oldHeader.ShadowCount;
                header.LightCount = oldHeader.LightCount;
                header.UserDataSize = oldHeader.UserDataSize;
                header.CompressionFlags = (CompressionFlags)oldHeader.CompressionFlags;
                header.MaxSolidIndex = oldHeader.MaxSolidIndex;
                header.DataSize = oldHeader.DataSize;
            }

            if (header.LightCount != 0 &&
                header.LightCount != 32)
            {
                throw new FormatException();
            }
            else if (header.ShadowCount != 0 &&
                header.ShadowCount != 8)
            {
                throw new FormatException();
            }

            this.AnimationTime = header.AnimationTime;
            this.Width = header.Width;
            this.Height = header.Height;
            this.RowCount = header.RowCount;
            this.ColumnCount = header.ColumnCount;
            this.ShadowCount = header.ShadowCount;
            this.LightCount = header.LightCount;
            this.MaxSolidIndex = header.MaxSolidIndex;
            this.CompressionFlags = header.CompressionFlags;

            for (int i = 0; i < this.Palette.Length; i++)
            {
                this.Palette[i] = input.ReadValueU32();
            }

            this.UserData = new byte[header.UserDataSize];
            if (input.Read(this.UserData, 0, this.UserData.Length) != this.UserData.Length)
            {
                throw new FormatException();
            }

            var infos = new FrameInfo[header.FrameCount];
            for (int i = 0; i < infos.Length; i++)
            {
                infos[i] = input.ReadStructure<FrameInfo>();
            }

            uint compressionFlags = (uint)header.CompressionFlags;
            if ((compressionFlags & ~0x1FFu) != 0)
            {
                throw new FormatException(String.Format("unknown compression flags in {0}", fileName));
            }
            else if (header.Unknown20 != 0 &&
                header.Unknown20 != 3)
            {
                // WHAT DOES THIS VALUE MEAN AUGH
                throw new NotSupportedException();
            }

            if ((header.CompressionFlags &
                CompressionFlags.NoCompression) != 0)
            {
                if ((header.CompressionFlags &
                    ~(CompressionFlags.NoPixels | CompressionFlags.NoCompression)) != 0)
                {
                    throw new FormatException(String.Format("other compression flags set with NoCompression flag in {0}", fileName));
                }
            }

            using (var data = input.ReadToMemoryStream(header.DataSize))
            {
                this.Frames = new Frame[header.FrameCount];
                for (int i = 0; i < header.FrameCount; i++)
                {
                    var info = infos[i];
                    data.Seek(info.Offset, SeekOrigin.Begin);

                    var frame = this.Frames[i] = new Frame();

                    frame.X = info.X;
                    frame.Y = info.Y;
                    frame.Width = Math.Abs(info.Width);
                    frame.Height = Math.Abs(info.Height);
                    frame.Pixels = new byte[frame.Width * frame.Height];

                    if ((header.CompressionFlags &
                        CompressionFlags.NoCompression) != 0)
                    {
                        // uncompressed data
                        data.Read(frame.Pixels, 0, frame.Pixels.Length);
                    }
                    else
                    {
                        // compressed data

                        var lengths = new int[frame.Height];
                        var max = 0;
                        for (int y = 0; y < frame.Height; y++)
                        {
                            int length = data.ReadValueU8();
                            if (length == 0xFF)
                            {
                                length = data.ReadValueU16();
                            }
                            lengths[y] = length;
                            max = Math.Max(max, length);
                        }

                        var scanline = new byte[max];
                        for (int y = 0, offset = 0; y < frame.Height; y++, offset = y * frame.Width)
                        {
                            var length = lengths[y];
                            if (data.Read(scanline, 0, length) != length)
                            {
                                throw new FormatException();
                            }

                            for (int x = 0; x < length; )
                            {
                                offset += (scanline[x] >> 4) & 0xF; // transparent
                                var literalCount = scanline[x] & 0xF;
                                if (literalCount > 0)
                                {
                                    Array.Copy(scanline, x + 1, frame.Pixels, offset, literalCount);
                                }
                                offset += literalCount;
                                x += 1 + literalCount;
                            }
                        }
                    }

                    // flip horizontal
                    if (info.Width < 0)
                    {
                        for (int y = 0, offset = 0; y < frame.Height; y++, offset += frame.Width)
                        {
                            Array.Reverse(frame.Pixels, offset, frame.Width);
                        }
                    }

                    // flip vertical
                    if (info.Height < 0)
                    {
                        var scanline = new byte[frame.Height];

                        for (int x = 0; x < frame.Width; x++)
                        {
                            for (int y = 0, offset = x; y < frame.Height; y++, offset += frame.Width)
                            {
                                scanline[y] = frame.Pixels[offset];
                            }

                            for (int y = 0, offset = x; y < frame.Height; y++, offset += frame.Width)
                            {
                                frame.Pixels[offset] = scanline[frame.Height - 1 - y];
                            }
                        }
                    }
                }
            }
        }
    }
}
