using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class UiartFont
        {
            public List<string> fonts = new List<string>();

            public UiartFont(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["UiartFont"];

                for (int i = 0; i <= 14; i++)
                {
                    fonts.Add(Parser.GetString(string.Format("Font{0}", i)));

                    //Load the blobs
                    BlobsToLoad.Add(Parser.GetBlob(fonts[i]));
                }
            }
        }
    }
}
