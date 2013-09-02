using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Assets;

namespace InfServer.Script.GameType_Fantasy
{
    public partial class RecipeInfo
    {

        public int id;          //Our recipe's ID
        public string name;      //Our recipe's name
        public string description;
        public Dictionary<int, int> ingredients;
        public int result;
        public int resultQuantity;


        public static List<RecipeInfo> Load(string filename)
        {
            List<RecipeInfo> recipeInfo = new List<RecipeInfo>();
            TextReader reader = new StreamReader(filename);
            string line = "";

            while ((line = reader.ReadLine()) != null)
            {
                List<string> values = CSVReader.Parse(line);

                Recipe recipe = Recipe.Load(values);
                recipeInfo.Add(recipe);
            }

            return recipeInfo;
        }

        public static void LoadGeneralSettings(RecipeInfo recipe, List<string> values)
        {
            //Read in our data
            recipe.id = CSVReader.GetInt(values[0]);
            recipe.name = CSVReader.GetQuotedString(values[2]);
            recipe.description = CSVReader.GetQuotedString(values[3]);

            recipe.ingredients = new Dictionary<int, int>();

            //Load in our ingredients
            for (int i = 4; i < 23; )
            {
                if (CSVReader.GetInt(values[i]) == 0)
                {
                    i = i + 2;
                    continue;
                }
                recipe.ingredients.Add(CSVReader.GetInt(values[i]), CSVReader.GetInt(values[i + 1]));
                i = i + 2;
            }

            //load results
            recipe.result = CSVReader.GetInt(values[24]);
            recipe.resultQuantity = CSVReader.GetInt(values[25]);
        }
    }
}
