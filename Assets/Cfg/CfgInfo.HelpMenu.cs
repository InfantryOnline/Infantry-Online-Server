using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
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
        public class HelpMenu
        {
            public List<Link> links = new List<Link>();

            public HelpMenu(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["HelpMenu"];

                for (int i = 0; i <= 29; i++)
                {
                    if (Parser.GetString(string.Format("Link{0}", i)) == "")
                        return;

                    links.Add(new Link(Parser.GetString(string.Format("Link{0}", i)).Split(',').GetValue(0).ToString().Replace("\"", ""),
                        Parser.GetString(string.Format("Link{0}", i)).Split(',').GetValue(1).ToString().Replace("\"", "")));
                }
            }
        }
    }
}
