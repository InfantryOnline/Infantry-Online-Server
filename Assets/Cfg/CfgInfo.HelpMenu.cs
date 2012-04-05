using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class HelpMenu
        {
            public struct Link
            {
                public string title, name;
                public Link(string fileTitle, string fileName)
                {
                    title = fileTitle;
                    name = fileName;
                }
            }
            public Link[] links = new Link[30];

            public HelpMenu(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["HelpMenu"];
                for (int i = links.GetLowerBound(0); i <= links.GetUpperBound(0); i++)
                {
                    if (Parser.GetString(string.Format("Link{0}", i)) == "")
                        return;
                    links[i].title = Parser.GetString(string.Format("Link{0}", i)).Split(',').GetValue(0).ToString().Replace("\"", "");
                    links[i].name = Parser.GetString(string.Format("Link{0}", i)).Split(',').GetValue(1).ToString().Replace("\"", "");
                }
            }
        }
    }
}
