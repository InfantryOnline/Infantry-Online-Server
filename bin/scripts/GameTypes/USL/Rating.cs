using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfServer.Script.GameType_USL
{
    public class EloRating
    {
        public double Point1 { get; set; }
        public double Point2 { get; set; }

        public double FinalResult1 { get; set; }
        public double FinalResult2 { get; set; }

        public EloRating(double CurrentRating1, double CurrentRating2, double Score1, double Score2)
        {
            /*
            double CurrentR1 = 1500.0;
            double CurrentR2 = 1500.0;
 
            double Score1 = 20.0;
            double Score2 = 10;
            */

            double E = 0;

            if (Score1 != Score2)
            {
                if (Score1 > Score2)
                {
                    E = 120 - Math.Round(1 / (1 + Math.Pow(10, ((CurrentRating2 - CurrentRating1) / 400))) * 120);
                    FinalResult1 = CurrentRating1 + E;
                    FinalResult2 = CurrentRating2 - E;
                }
                else
                {
                    E = 120 - Math.Round(1 / (1 + Math.Pow(10, ((CurrentRating1 - CurrentRating2) / 400))) * 120);
                    FinalResult1 = CurrentRating1 - E;
                    FinalResult2 = CurrentRating2 + E;
                }
            }
            else
            {
                if (CurrentRating1 == CurrentRating2)
                {
                    FinalResult1 = CurrentRating1;
                    FinalResult2 = CurrentRating2;
                }
                else
                {
                    if (CurrentRating1 > CurrentRating2)
                    {
                        E = (120 - Math.Round(1 / (1 + Math.Pow(10, ((CurrentRating1 - CurrentRating2) / 400))) * 120)) - (120 - Math.Round(1 / (1 + Math.Pow(10, ((CurrentRating2 - CurrentRating1) / 400))) * 120));
                        FinalResult1 = CurrentRating1 - E;
                        FinalResult2 = CurrentRating2 + E;
                    }
                    else
                    {
                        E = (120 - Math.Round(1 / (1 + Math.Pow(10, ((CurrentRating2 - CurrentRating1) / 400))) * 120)) - (120 - Math.Round(1 / (1 + Math.Pow(10, ((CurrentRating1 - CurrentRating2) / 400))) * 120));
                        FinalResult1 = CurrentRating1 + E;
                        FinalResult2 = CurrentRating2 - E;
                    }
                }
            }
            Point1 = FinalResult1 - CurrentRating1;
            Point2 = FinalResult2 - CurrentRating2;

        }
    }
}
