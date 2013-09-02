using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Assets;

namespace InfServer.Script.GameType_Fantasy
{
        public partial class BookInfo
        {

            public enum BookType
            {
                Spell = 1,
            }

            public int ID;          //Our book's ID
            public int itmID;       //The ID of the Open Book Item.
            public string openText; //Text displayed to the player on open.
            public BookType bookType;


            public static List<BookInfo> Load(string filename)
            {
                List<BookInfo> bookInfo = new List<BookInfo>();
                TextReader reader = new StreamReader(filename);
                string line = "";

                while ((line = reader.ReadLine()) != null)
                {
                    List<string> values = CSVReader.Parse(line);
                    switch (values[0])
                    {
                        case "1":
                            {
                                SpellBook spellbook = SpellBook.Load(values);
                                bookInfo.Add(spellbook);
                            }
                            break;
                    }
                }

                return bookInfo;
            }

        public static void LoadGeneralSettings(BookInfo book, List<string> values)
        {
            //Read in our data
            book.bookType = (BookType)CSVReader.GetInt(values[0]);
            book.ID = CSVReader.GetInt(values[1]);
            book.itmID = CSVReader.GetInt(values[2]);
            book.openText = CSVReader.GetQuotedString(values[3]);
        }
    }

}
