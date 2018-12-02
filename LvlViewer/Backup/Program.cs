using System;
using System.Collections.Generic;
using Assets;

namespace LvlViewer
{
    /// <summary>
    /// A minimal .lvl reader. Very little calculation is performed, most of the work is done by the parser.
    /// </summary>
    class Program
    {
        /// <summary>
        /// 0-31 english-friendly values for each type of physics overlay.
        /// </summary>
        private static readonly List<string> physValues = 
            new List<string>
            {
                "Clear",

                "Red Solid","Red Upper Left","Red Upper Right","Red Lower Left","Red Lower Right",

                "Green Solid","Green Upper Left","Green Upper Right","Green Lower Left","Green Lower Right",

                "Yellow Solid","Yellow Upper Left","Yellow Upper Right","Yellow Lower Left","Yellow Lower Right",

                "Orange Solid","Orange Upper Left","Orange Upper Right","Orange Lower Left","Orange Lower Right",

                "Purple Solid","Purple Upper Left","Purple Upper Right","Purple Lower Left","Purple Lower Right",

                "Red Move Right", "Red Move Left", "Red Move Down", "Red Move Up",

                "Teal Solid",

                "Blue Solid"
            };

        /// <summary>
        /// Load and print
        /// </summary>
        /// <param name="args">args[0] must be the level file.</param>
        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                throw new ArgumentException("No .lvl file given");
            }

            LvlInfo level = LvlInfo.Load(args[0]);

            Print("File Opened", delegate { PrintValue("Name", args[0]); } );

            Print("General", 
                delegate
                {
                    PrintValue("Width", level.Width);
                    PrintValue("Height", level.Height);
                });

            Print("Physics", 
                delegate
                {
                    PrintValue("Low", level.PhysicsLow);
                    PrintValue("High", level.PhysicsHigh);
                });

            Print("Blob References (Filename, Id)", 
                delegate
                    {
                        PrintValue("Object Blobs", level.ObjectBlobs);
                        PrintValue("Floor Blobs", level.FloorBlobs);
                    });

            Print("Tile References (Terrain#, UNKNOWN, Physics/Vision)",delegate{ PrintValue("Tiles", level.Tiles); });
        }

        static void Print(string title, Action action)
        {
            Console.WriteLine("===== " + title + " =====");
            action();
            Console.WriteLine();
        }

        static void PrintValue<T>(string name, T value)
        {
            Console.WriteLine("[" + name + "]\t" + value);
        }

        static void PrintValue<T>(string name, T[] values)
        {
            PrintValue(name, "");

            for(int idx = 0; idx < values.Length; idx++)
            {
                Console.Write("\t");
                PrintValue(idx.ToString(), values[idx]);
            }
        }

        static void PrintValue(string name, LvlInfo.BlobReference[] values)
        {
            PrintValue(name, "");
            for(int idx = 0; idx < values.Length; idx++)
            {
                Console.Write("\t");
                PrintValue(idx.ToString(), values[idx].FileName + "\t\t" + values[idx].Id);
            }
        }

        static void PrintValue(string name, LvlInfo.Tile[] values)
        {
            PrintValue(name, "");

            for(int idx = 0; idx < values.Length; idx++)
            {
                String s = String.Format("{0,-12}{1,-10}{2}", "Terrain" + values[idx].Unknown0, values[idx].Unknown1,
                                         physValues[values[idx].Unknown2 & 0x1F] + "/" + (values[idx].Unknown2 >> 5));
                Console.Write("\t");
                PrintValue(idx.ToString(), s);
            }
        }
    }
}
