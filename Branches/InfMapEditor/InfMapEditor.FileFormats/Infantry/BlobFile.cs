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
using System.Collections.Generic;
using System.IO;
using Gibbed.Helpers;

namespace InfMapEditor.FileFormats.Infantry
{
    public class BlobFile
    {
        public uint Version;
        public List<Entry> Entries
            = new List<Entry>();

        public void Serialize(Stream output)
        {
            if (this.Version < 1 || this.Version > 2)
            {
                throw new InvalidOperationException("unsupported blob version");
            }

            output.WriteValueU32(this.Version);
            output.WriteValueS32(this.Entries.Count);

            var nameLength = this.Version == 2 ? 32 : 14;

            foreach (var entry in this.Entries)
            {
                var name = entry.Name;

                if (name.Length + 1 > nameLength)
                {
                    throw new InvalidOperationException();
                }

                output.WriteString(name.PadRight(nameLength, '\0'));
                output.WriteValueU32((uint)entry.Offset);
                output.WriteValueU32(entry.Size);
            }
        }

        public void Deserialize(Stream input)
        {
            this.Version = input.ReadValueU32();
            if (this.Version < 1 || this.Version > 2)
            {
                throw new FormatException("unsupported blob version");
            }

            var nameLength = this.Version == 2 ? 32u : 14u;

            var count = input.ReadValueU32();
            this.Entries = new List<Entry>();
            for (uint i = 0; i < count; i++)
            {
                var entry = new Entry();
                entry.Name = input.ReadString(nameLength, true);
                entry.Offset = input.ReadValueU32();
                entry.Size = input.ReadValueU32();
                this.Entries.Add(entry);
            }
        }

        public struct Entry
        {
            public string Name;
            public long Offset;
            public uint Size;
        }
    }
}
