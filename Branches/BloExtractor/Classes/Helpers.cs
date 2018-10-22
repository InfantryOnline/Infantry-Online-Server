using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using Gibbed.Helpers;
using Gibbed.Infantry.FileFormats;
using System.IO;
using System.Runtime.InteropServices;

namespace BloExtractor
{
    public partial class Helpers
    {
        public static void GenerateCFSPreview(string inputFile, string outputFile, bool dontFixSpecialColors = false)
        {
            SpriteFile sprite;
            using (var input = File.OpenRead(inputFile))
            {
                sprite = new SpriteFile();
                sprite.Deserialize(input);
            }

            var bitmap = new Bitmap(
                sprite.Width,
                sprite.Height,
                PixelFormat.Format8bppIndexed);

            var palette = bitmap.Palette;
            var shadowIndex = 256 - sprite.ShadowCount;
            var lightIndex = shadowIndex - sprite.LightCount;

            for (int i = 0; i < 256; i++)
            {
                var color = sprite.Palette[i];

                var r = (int)((color >> 16) & 0xFF);
                var g = (int)((color >> 8) & 0xFF);
                var b = (int)((color >> 0) & 0xFF);
                //var a = (int)((color >> 24) & 0xFF);

                int a;

                if (i == 0)
                {
                    // transparent pixel
                    a = 0;
                }
                else if (sprite.ShadowCount > 0 && i >= shadowIndex)
                {
                    if (dontFixSpecialColors == false)
                    {
                        // make shadows black+alpha
                        a = 64 + (((i - shadowIndex) + 1) * 16);
                        r = g = b = 0;
                    }
                    else
                    {
                        a = 255;
                    }
                }
                else if (sprite.LightCount > 0 && i >= lightIndex)
                {
                    if (dontFixSpecialColors == false)
                    {
                        // make lights white+alpha
                        a = 64 + (((i - lightIndex) + 1) * 4);
                        r = g = b = 255;
                    }
                    else
                    {
                        a = 255;
                    }
                }
                /*else if (i > sprite.MaxSolidIndex)
                {
                    a = 0;
                }*/
                else
                {
                    a = 255;
                }

                palette.Entries[i] = Color.FromArgb(a, r, g, b);
            }
            bitmap.Palette = palette;

            for (int i = 0, y = 0; y < sprite.Height; y += sprite.Height)
            {
                for (int x = 0; x < sprite.Width; x += sprite.Width)
                {
                    try
                    {
                        var frame = sprite.Frames[0];

                        if (frame.Width == 0 ||
                            frame.Height == 0)
                        {
                            continue;
                        }

                        var area = new Rectangle(
                            x + frame.X, y + frame.Y,
                            frame.Width, frame.Height);

                        var data = bitmap.LockBits(area, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                        var scan = data.Scan0;
                        for (int o = 0; o < frame.Height * frame.Width; o += frame.Width)
                        {
                            Marshal.Copy(frame.Pixels, o, scan, frame.Width);
                            scan += data.Stride;
                        }
                        bitmap.UnlockBits(data);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            bitmap.Save(outputFile, ImageFormat.Png);
        }
    }
}
