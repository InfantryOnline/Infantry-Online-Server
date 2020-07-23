using System;
using System.Collections.Generic;
using System.IO;

namespace InfServer.Script.GameType_Multi
{
    public static class ListReader
    {

        public static List<string> readListFromFile(string filename)
        {
            if (!File.Exists(System.Environment.CurrentDirectory + "\\" + filename))
                File.Create(System.Environment.CurrentDirectory + "\\" + filename);

            List<string> newList = new List<string>();

            using (StreamReader r = new StreamReader(System.Environment.CurrentDirectory + "\\" + filename))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                {
                    newList.Add(line);
                }
            }
            return newList;
        }
        public static void saveListToFile(List<String> list, string filename)
        {
            if (!File.Exists(System.Environment.CurrentDirectory + "\\" + filename))
                File.Create(System.Environment.CurrentDirectory + "\\" + filename);

            File.WriteAllLines(System.Environment.CurrentDirectory + "\\" + filename, list);
        }
    }
}
