using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets;

namespace InfServer.Script.GameType_Fantasy
{
    public partial class BookInfo
    {
        public class SpellBook : BookInfo
        {
            public static SpellBook Load(List<string> values)
            {
                SpellBook spellbook = new SpellBook();
                BookInfo.LoadGeneralSettings(spellbook, values);
                return spellbook;
            }
        }
    }
}
