using System.Collections.Generic;

namespace Assets
{
    public partial class CfgInfo
    {
        public class Render
        {
            public bool Static;
            public string backgroundColor;
            public bool flattenLights;
            public bool trailsFirst;
            public bool vehTrailsFirst;
            public int starfieldGapNear;
            public int starfieldGapFar;

            public Render(ref Dictionary<string, Dictionary<string, string>> stringTree)
            {
                Parser.values = stringTree["Render"];

                Static = Parser.GetBool("Static");
                backgroundColor = Parser.GetString("BackgroundColor");
                flattenLights = Parser.GetBool("FlattenLights");
                trailsFirst = Parser.GetBool("TrailsFirst");
                vehTrailsFirst = Parser.GetBool("VehTrailsFirst");
                starfieldGapNear = Parser.GetInt("StarfieldGapNear");
                starfieldGapFar = Parser.GetInt("StarfieldGapFar");
            }
        }
    }
}
