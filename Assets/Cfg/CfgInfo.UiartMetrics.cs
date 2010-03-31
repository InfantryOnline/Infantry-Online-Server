using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class UiartMetrics
        {
            public int messageBorderFirst;
            public string messageFill;
            public int messageBorderLeft;
            public int messageBorderRight;
            public int messageBorderTop;
            public string mainFill;
            public string gapFill;
            public string notepadFill;
            public int notepadListBoxLeft;
            public int notepadListBoxTop;
            public int notepadListBoxBottom;
            public int notepadListTextLeft;
            public int notepadListTextWidth;
            public int playerListBoxTopHeight;
            public int playerListBoxBottomHeight;
            public int inventoryListBoxTopHeight;
            public int inventoryListBoxBottomHeight;
            public int displayEncumbrance;
            public int menuBounce;
            public string menuBorderColor;
            public string scoreBorderColor;
            public int guageEnergyX;
            public int guageEnergyY;
            public int guageEnergyTX;
            public int guageEnergyTY;
            public int guageHealthX;
            public int guageHealthY;
            public int guageHealthTX;
            public int guageHealthTY;
            public int guageUnitX;
            public int guageUnitY;
            public int guageUnitTX;
            public int guageUnitTY;
            public int guageFont;
            public int guageFontColor;
            public int menuFont;
            public int menuFontColor;
            public int menuSelectFont;
            public int menuSelectFontColor;
            public int menuTitleFont;
            public int menuTitleFontColor;
            public int menuDisabledFont;
            public int menuDisabledFontColor;
            public int menuButtonSize;
            public int keystrokeBubbleButtonSize;
            public int keystrokeBubbleFont;
            public int keystrokeBubbleFontColor;
            public int playerListCountFont;
            public int playerListCountFontColor;
            public int playerListCountLocationX;
            public int playerListCountLocationY;
            public string playerListCountString;
            public string menuFillColor;
            public int buttonSize;
            public int buttonFont;
            public int buttonFontColor;
            public int buttonHighlightFont;
            public int buttonHighlightFontColor;
            public int flagStatusBubbleButtonSize;
            public int flagStatusBubbleFont;
            public int flagStatusBubbleFontColor;

            public UiartMetrics(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["UiartMetrics"];

                messageBorderFirst = Parser.GetInt("MessageBorderFirst");
                messageFill = Parser.GetString("MessageFill");
                messageBorderLeft = Parser.GetInt("MessageBorderLeft");
                messageBorderRight = Parser.GetInt("MessageBorderRight");
                messageBorderTop = Parser.GetInt("MessageBorderTop");
                mainFill = Parser.GetString("MainFill");
                gapFill = Parser.GetString("GapFill");
                notepadFill = Parser.GetString("NotepadFill");
                notepadListBoxLeft = Parser.GetInt("NotepadListBoxLeft");
                notepadListBoxTop = Parser.GetInt("NotepadListBoxTop");
                notepadListBoxBottom = Parser.GetInt("NotepadListBoxBottom");
                notepadListTextLeft = Parser.GetInt("NotepadListTextLeft");
                notepadListTextWidth = Parser.GetInt("NotepadListTextWidth");
                playerListBoxTopHeight = Parser.GetInt("PlayerListBoxTopHeight");
                playerListBoxBottomHeight = Parser.GetInt("PlayerListBoxBottomHeight");
                inventoryListBoxTopHeight = Parser.GetInt("InventoryListBoxTopHeight");
                inventoryListBoxBottomHeight = Parser.GetInt("InventoryListBoxBottomHeight");
                displayEncumbrance = Parser.GetInt("DisplayEncumbrance");
                menuBounce = Parser.GetInt("MenuBounce");
                menuBorderColor = Parser.GetString("MenuBorderColor");
                scoreBorderColor = Parser.GetString("ScoreBorderColor");
                guageEnergyX = Parser.GetInt("GuageEnergyX");
                guageEnergyY = Parser.GetInt("GuageEnergyY");
                guageEnergyTX = Parser.GetInt("GuageEnergyTX");
                guageEnergyTY = Parser.GetInt("GuageEnergyTY");
                guageHealthX = Parser.GetInt("GuageHealthX");
                guageHealthY = Parser.GetInt("GuageHealthY");
                guageHealthTX = Parser.GetInt("GuageHealthTX");
                guageHealthTY = Parser.GetInt("GuageHealthTY");
                guageUnitX = Parser.GetInt("GuageUnitX");
                guageUnitY = Parser.GetInt("GuageUnitY");
                guageUnitTX = Parser.GetInt("GuageUnitTX");
                guageUnitTY = Parser.GetInt("GuageUnitTY");
                guageFont = Parser.GetInt("GuageFont");
                guageFontColor = Parser.GetInt("GuageFontColor");
                menuFont = Parser.GetInt("MenuFont");
                menuFontColor = Parser.GetInt("MenuFontColor");
                menuSelectFont = Parser.GetInt("MenuSelectFont");
                menuSelectFontColor = Parser.GetInt("MenuSelectFontColor");
                menuTitleFont = Parser.GetInt("MenuTitleFont");
                menuTitleFontColor = Parser.GetInt("MenuTitleFontColor");
                menuDisabledFont = Parser.GetInt("MenuDisabledFont");
                menuDisabledFontColor = Parser.GetInt("MenuDisabledFontColor");
                menuButtonSize = Parser.GetInt("MenuButtonSize");
                keystrokeBubbleButtonSize = Parser.GetInt("KeystrokeBubbleButtonSize");
                keystrokeBubbleFont = Parser.GetInt("KeystrokeBubbleFont");
                keystrokeBubbleFontColor = Parser.GetInt("KeystrokeBubbleFontColor");
                playerListCountFont = Parser.GetInt("PlayerListCountFont");
                playerListCountFontColor = Parser.GetInt("PlayerListCountFontColor");
                playerListCountLocationX = Parser.GetInt("PlayerListCountLocationX");
                playerListCountLocationY = Parser.GetInt("PlayerListCountLocationY");
                playerListCountString = Parser.GetString("PlayerListCountString");
                menuFillColor = Parser.GetString("MenuFillColor");
                buttonSize = Parser.GetInt("ButtonSize");
                buttonFont = Parser.GetInt("ButtonFont");
                buttonFontColor = Parser.GetInt("ButtonFontColor");
                buttonHighlightFont = Parser.GetInt("ButtonHighlightFont");
                buttonHighlightFontColor = Parser.GetInt("ButtonHighlightFontColor");
                flagStatusBubbleButtonSize = Parser.GetInt("FlagStatusBubbleButtonSize");
                flagStatusBubbleFont = Parser.GetInt("FlagStatusBubbleFont");
                flagStatusBubbleFontColor = Parser.GetInt("FlagStatusBubbleFontColor");
            }
        }
    }
}
