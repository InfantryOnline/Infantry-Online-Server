using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using Gibbed.Helpers;
using Gibbed.Infantry.FileFormats;
using System.Runtime.InteropServices;

namespace BloExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            string infPath;

            if (args.Length == 0)
                infPath = Directory.GetCurrentDirectory();
            else
                infPath = args[0];

            var md5 = MD5.Create();
            var extractLocation = Path.Combine(infPath, "BloExtraction");

            if(Directory.Exists(extractLocation))
                Directory.CreateDirectory(extractLocation);

            Console.SetCursorPosition(0, 0);
            Console.Write("Blo Extractor");
            Console.SetCursorPosition(0, 2);
            Console.Write("Working directory: " + infPath);

            foreach (var inputPath in Directory.GetFiles(infPath, "*.blo"))
            {
                var bloName = Path.GetFileName(inputPath);
                var bloLocation = Path.Combine(extractLocation, bloName);

                if (!Directory.Exists(bloLocation))
                    Directory.CreateDirectory(bloLocation);

                using (var input = File.OpenRead(inputPath))
                {
                    var blob = new BlobFile();
                    blob.Deserialize(input);

                    Console.SetCursorPosition(0, 3);
                    Console.Write(String.Format("Current blo: {0}", bloName).PadRight(Console.WindowWidth));

                    foreach (var cfsEntry in blob.Entries)
                    {
                        input.Seek(cfsEntry.Offset, SeekOrigin.Begin);

                        var data = new byte[cfsEntry.Size];
                        input.Read(data, 0, data.Length);

                        var hash = md5.ComputeHash(data);
                        var friendlyHash = BitConverter
                            .ToString(hash)
                            .Replace("-", "")
                            .ToLowerInvariant();

                        Console.SetCursorPosition(0, 4);
                        Console.Write(String.Format("Current cfs: {0}", cfsEntry.Name).PadRight(Console.WindowWidth));

                        //Output the cfs file
                        string cfsFile = Path.Combine(bloLocation, cfsEntry.Name);
                        using (var output = File.Create(cfsFile))
                        {
                            output.Write(data, 0, data.Length);
                        }

                        //Output a settings file
                        using (var outputinfo = File.CreateText(Path.Combine(bloLocation, cfsEntry.Name + "-info.txt")))
                        {
                            outputinfo.WriteLine("name=" + cfsEntry.Name);
                            outputinfo.WriteLine("size=" + cfsEntry.Size);
                            outputinfo.WriteLine("md5=" + friendlyHash);
                            outputinfo.WriteLine("blo=" + bloName);
                            outputinfo.WriteLine("newname=none");
                            outputinfo.WriteLine("newblo=none");
                        }

                        //Lets try to create a sample image file, too
                        if (Path.GetExtension(cfsEntry.Name) != ".cfs")
                            continue;

                        string previewFile = Path.Combine(bloLocation, cfsEntry.Name + "-preview.png");
                        Helpers.GenerateCFSPreview(cfsFile, previewFile, true);
                    }
                }
            }
            Console.SetCursorPosition(0, 6);
            Console.Write("Blo Extraction complete...");
            Console.ReadKey();
        }
    }
}
