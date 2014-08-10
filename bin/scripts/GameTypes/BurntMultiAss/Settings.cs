using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfServer.Script.GameType_Burnt
{
    public class Settings
    {
        public bool EventsEnabled = false;
        public static bool VotingEnabled = true;
        
        public static int VotingPeriod = 40;
        public static int MinPlayers = 4;
        public static int GamesBeforeEvent = 10;

        public static List<GameTypes> AllowedGameTypes = new List<GameTypes>();

        public GameStates GameState = GameStates.Init;

        public CTFMode CTFMode = CTFMode.NotEnoughPlayers;

        /// <summary>
        /// Gets the type based on a string used
        /// </summary>
        public byte GetType(string type)
        {
            if (String.IsNullOrWhiteSpace(type))
                return (byte)GameTypes.NULL;

            string lower = type.ToLower();
            byte value = (byte)GameTypes.TDM;
            switch (lower)
            {
                case "tdm":
                    value = (byte)GameTypes.TDM;
                    break;
                case "koth":
                    value = (byte)GameTypes.KOTH;
                    break;
                case "glad":
                    value = (byte)GameTypes.GLAD;
                    break;
                case "ctf":
                    value = (byte)GameTypes.CTF;
                    break;
            }

            return value;
        }
    }

    public enum GameTypes
    {
        NULL,
        REPEAT,
        KOTH,
        TDM,
        CTF,
        GLAD,
    }

    public enum GameStates
    {
        Init,
        Vote,
        PreGame,
        ActiveGame,
        PostGame,
    }

    public enum CTFMode
    {
        ActiveGame,
        NotEnoughPlayers,
        Init,
        TenSeconds,
        ThirtySeconds,
        SixtySeconds,
        XSeconds,
        GameDone,
    }
}
