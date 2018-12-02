using System.Collections.Generic;

namespace Parsers
{
    public static class VehiclesFile
    {
        private static Vehicle Instantiate(int id)
        {
            switch (id)
            {
                case 2: return new Vehicle.Car();
                case 3: return new Vehicle.Dependent();
                case 4: return new Vehicle.Spectator();
                case 5: return new Vehicle.Computer();
                case 6: return new Vehicle.Nested();
            }

            return null;
        }

        public static List<Vehicle> Load(string path)
        {
            return CsvFile<Vehicle>.Load(path, Instantiate, false);
        }
    }
}
