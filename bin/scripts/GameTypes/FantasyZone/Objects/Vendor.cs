using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets;

namespace InfServer.Script.GameType_Fantasy
{
    public class Vendor
    {
        public int vehID; //Our linked Vehicle ID
        public VehInfo.Computer _vehicle;   //The specific vehicle we belong to

        public Vendor()
        {
        }
    }
}
