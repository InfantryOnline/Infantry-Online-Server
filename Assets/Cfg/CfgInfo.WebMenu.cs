using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class WebMenu
        {
            public List<Link> links = new List<Link>();

            public WebMenu(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["WebMenu"];

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
