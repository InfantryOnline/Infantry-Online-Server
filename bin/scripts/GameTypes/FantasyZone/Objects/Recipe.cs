using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets;

namespace InfServer.Script.GameType_Fantasy
{
    public partial class RecipeInfo
    {
        public class Recipe : RecipeInfo
        {
            public static Recipe Load(List<string> values)
            {
                Recipe recipe = new Recipe();
                Recipe.LoadGeneralSettings(recipe, values);
                return recipe;
            }
        }
    }
}
