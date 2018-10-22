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

using System;
using System.IO;
using Gibbed.Helpers;

namespace InfMapEditor.FileFormats.Infantry
{
    public static class StreamHelpers
    {
        public static int WriteRLE(this Stream stream, byte[] data, int size, int count, bool encodedLengths)
        {
            int maxRun = encodedLengths == false ? 0xFF : 0xFFFF;
            int totalBytes = 0;

            for (int stage = 0; stage < size; stage++)
            {
                int offset = stage;

                int left = count;
                while (left > 0)
                {
                    var value = data[offset];
                    offset += size;

                    int repeat = 1;
                    for (; repeat < maxRun && offset < data.Length; offset += size)
                    {
                        if (data[offset] != value)
                        {
                            break;
                        }

                        repeat++;
                    }

                    if (repeat <= 0xFF)
                    {
                        stream.WriteValueU8((byte)repeat);
                        totalBytes++;
                    }
                    else if (encodedLengths == false)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        stream.WriteValueU8(0);
                        stream.WriteValueU8((byte)((repeat >> 8) & 0xFF));
                        stream.WriteValueU8((byte)((repeat >> 0) & 0xFF));
                        totalBytes += 3;
                    }

                    stream.WriteValueU8(value);
                    totalBytes++;

                    left -= repeat;
                }
            }

            return totalBytes;
        }

        public static byte[] ReadRLE(this Stream stream, int size, int count, bool encodedLengths)
        {
            var data = new byte[count * size];

            for (int stage = 0; stage < size; stage++)
            {
                int offset = stage;

                int left = count;
                while (left > 0)
                {
                    int repeat = stream.ReadValueU8();
                    if (encodedLengths == true && repeat == 0)
                    {
                        repeat = stream.ReadValueU8() << 8;
                        repeat |= stream.ReadValueU8();
                    }

                    left -= repeat;

                    var value = stream.ReadValueU8();
                    for (; repeat > 0; offset += size)
                    {
                        data[offset] = value;
                        repeat--;
                    }
                }
            }

            return data;
        }
    }
}
