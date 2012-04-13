using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Uiwav
        {
            public string melt;
            public string changeIn;
            public string rClick;
            public string cantFire;
            public string pickMenu;
            public string warpIn;
            public string noAmmo;
            public string pickup;

            public Uiwav(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Uiwav"];

                melt = Parser.GetString("Melt");
                changeIn = Parser.GetString("ChangeIn");
                rClick = Parser.GetString("RClick");
                cantFire = Parser.GetString("CantFire");
                pickMenu = Parser.GetString("PickMenu");
                warpIn = Parser.GetString("WarpIn");
                noAmmo = Parser.GetString("NoAmmo");
                pickup = Parser.GetString("Pickup");
                
                //Load the blobs
                BlobsToLoad.Add(Parser.GetBlob(melt));
                BlobsToLoad.Add(Parser.GetBlob(changeIn));
                BlobsToLoad.Add(Parser.GetBlob(rClick));
                BlobsToLoad.Add(Parser.GetBlob(cantFire));
                BlobsToLoad.Add(Parser.GetBlob(pickMenu));
                BlobsToLoad.Add(Parser.GetBlob(warpIn));
                BlobsToLoad.Add(Parser.GetBlob(noAmmo));
                BlobsToLoad.Add(Parser.GetBlob(pickup));
            }
        }
    }
}
