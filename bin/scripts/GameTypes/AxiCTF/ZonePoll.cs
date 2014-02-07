using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Game;

namespace InfServer.Script.GameType_AxiCTF
{
    public class ZonePoll
    {
        //probably not do this
        public class ZoneChoice
        {
            public int x1;
            public int y1;

            public int x2, y2;

            public int id;
            public int tally1;
            public int tally2;

            public string description;

            public ZoneChoice(int id)
            {
                this.id = id;
                tally1 = 0;
                tally2 = 0;
            }
        }

        private class Ballot
        {
            public string voter;
            public int vote;
        }

        private List<Ballot> votes;
        private List<ZoneChoice> zoneChoices;
        public List<int> pollingLocations;

        public bool isActive;

        public int _votingTime = 2000;

        public ZonePoll()
        {
            Initiate();
        }

        public void Initiate()
        {
            votes = new List<Ballot>();
            zoneChoices = new List<ZoneChoice>();

            zoneChoices.Add(new ZoneChoice(0));
            zoneChoices[0].x1 = 525;
            zoneChoices[0].y1 = 713;
            zoneChoices[0].x2 = 589;
            zoneChoices[0].y2 = 523;

            zoneChoices.Add(new ZoneChoice(1));
            zoneChoices[1].x1 = 2409;
            zoneChoices[1].y1 = 621;
            zoneChoices[1].x2 = 2785;
            zoneChoices[1].y2 = 161;

            zoneChoices.Add(new ZoneChoice(2));
            zoneChoices[2].x1 = 3817;
            zoneChoices[2].y1 = 513;
            zoneChoices[2].x2 = 3501;
            zoneChoices[2].y2 = 761;

            zoneChoices.Add(new ZoneChoice(3));
            zoneChoices[3].x1 = 5017;
            zoneChoices[3].y1 = 156;
            zoneChoices[3].x2 = 5065;
            zoneChoices[3].y2 = 496;

            zoneChoices.Add(new ZoneChoice(4));
            zoneChoices[4].x1 = 6261;
            zoneChoices[4].y1 = 476;
            zoneChoices[4].x2 = 7861;
            zoneChoices[4].y2 = 176;

            zoneChoices.Add(new ZoneChoice(5));
            zoneChoices[5].x1 = 8609;
            zoneChoices[5].y1 = 324;
            zoneChoices[5].x2 = 8969;
            zoneChoices[5].y2 = 180;

            zoneChoices.Add(new ZoneChoice(6));
            zoneChoices[6].x1 = 668;
            zoneChoices[6].y1 = 2324;
            zoneChoices[6].x2 = 500;
            zoneChoices[6].y2 = 2200;

            zoneChoices.Add(new ZoneChoice(7));
            zoneChoices[7].x1 = 1724;
            zoneChoices[7].y1 = 1552;
            zoneChoices[7].x2 = 2120;
            zoneChoices[7].y2 = 2360;

            zoneChoices.Add(new ZoneChoice(8));
            zoneChoices[8].x1 = 3828;
            zoneChoices[8].y1 = 1944;
            zoneChoices[8].x2 = 3544;
            zoneChoices[8].y2 = 1784;

            zoneChoices.Add(new ZoneChoice(9));
            zoneChoices[9].x1 = 5448;
            zoneChoices[9].y1 = 2228;
            zoneChoices[9].x2 = 5088;
            zoneChoices[9].y2 = 1752;

            zoneChoices.Add(new ZoneChoice(10));
            zoneChoices[10].x1 = 6264;
            zoneChoices[10].y1 = 2488;
            zoneChoices[10].x2 = 6432;
            zoneChoices[10].y2 = 1644;

            zoneChoices.Add(new ZoneChoice(11));
            zoneChoices[11].x1 = 7680;
            zoneChoices[11].y1 = 2380;
            zoneChoices[11].x2 = 7792;
            zoneChoices[11].y2 = 2500;

            zoneChoices.Add(new ZoneChoice(12));
            zoneChoices[12].x1 = 1520;
            zoneChoices[12].y1 = 3956;
            zoneChoices[12].x2 = 648;
            zoneChoices[12].y2 = 3184;

            zoneChoices.Add(new ZoneChoice(13));
            zoneChoices[13].x1 = 2380;
            zoneChoices[13].y1 = 3084;
            zoneChoices[13].x2 = 2232;
            zoneChoices[13].y2 = 3184;

            zoneChoices.Add(new ZoneChoice(14));
            zoneChoices[14].x1 = 4024;
            zoneChoices[14].y1 = 2924;
            zoneChoices[14].x2 = 3704;
            zoneChoices[14].y2 = 3992;

            zoneChoices.Add(new ZoneChoice(15));
            zoneChoices[15].x1 = 5572;
            zoneChoices[15].y1 = 3952;
            zoneChoices[15].x2 = 5588;
            zoneChoices[15].y2 = 3280;

        }

        public void removeChoice(int id)
        {
            zoneChoices.RemoveAt(id);
        }

        public void restart()
        {
            votes = new List<Ballot>();
            zoneChoices = new List<ZoneChoice>();
        }
        //do something else, not out 
        public void getMapCoords(out ZoneChoice team1, out ZoneChoice team2)
        {

            int id1, id2;
            getWinner(out id1, out id2);

            if (id1 == id2)
            {
                //randomize who gets increased
                if (id2 == 15)
                    id2 = 0;
                else
                    id2++;
            }

            team1 = zoneChoices.Find(f => f.id == id1);
            team2 = zoneChoices.Find(f => f.id == id2);
        }

        private ZoneChoice getZoneChoiceByID(int id)
        {
            return zoneChoices.Find(f => f.id == id);
        }

        public bool addVote(string voter, int voteID, string team)
        {
            if (voteID >= zoneChoices.Count)
            {
                return false;
            }

            foreach (Ballot b in votes)
            {
                if (b.voter == voter)
                {
                    //Change their vote if they already voted
                    if (team.Contains("spec"))
                    {
                        return false;
                    }

                    if (team.Contains("Titan"))
                    {
                        zoneChoices.Find(f => f.id == voteID).tally1++;

                        //Find their old vote and remove it
                        zoneChoices.Find(f => f.id == b.vote).tally1--;
                    }

                    if (team.Contains("Collective"))
                    {
                        zoneChoices.Find(f => f.id == voteID).tally2++;
                        zoneChoices.Find(f => f.id == b.vote).tally1--;
                    }

                    return true;
                }
               
            }

            Ballot ballot = new Ballot();

            ballot.voter = voter;
            ballot.vote = voteID;

            votes.Add(ballot);

            if (team.Contains("spec"))
            {
                return false;
            }

            if (team.Contains("Titan"))
            {
                zoneChoices.Find(f => f.id == voteID).tally1++;

            }

            if (team.Contains("Collective"))
            {
                zoneChoices.Find(f => f.id == voteID).tally2++;
            }

            return true;
        }

        private void getWinner(out int c1ID, out int c2ID)
        {
            int max1 = 0;
            int max2 = 0;

            c1ID = 0; //Team 1 home
            c2ID = 0; //Team 2 home

            foreach (ZoneChoice c in zoneChoices)
            {
                if (max1 < c.tally1)
                {
                    max1 = c.tally1;
                    c1ID = c.id;
                }

                if (max2 < c.tally2)
                {
                    max2 = c.tally2;
                    c2ID = c.id;
                }

            }

        }

    }
}
