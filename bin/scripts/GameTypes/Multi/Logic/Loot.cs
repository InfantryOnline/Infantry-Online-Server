using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Security.Cryptography;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;
using InfServer.Network;
using InfServer.Data;

using Assets;

namespace InfServer.Script.GameType_Multi
{
    public class Loot
    {
        Dictionary<int, LootTable> _lootTables;
        private Random _rand;
        private Arena _arena;
        private LogClient _logger;				//Our log client for loot related activity
        private Script_Multi _script;

        public Loot(Arena arena, Script_Multi script)
        {
            _script = script;
            _arena = arena;
            _logger = Log.createClient(String.Format("Loot_{0}", arena._name));
        }

        public void Load(ZoneServer server)
        {
            Log.write("Loading loot tables...");
            _rand = new Random();
            _lootTables = new Dictionary<int, LootTable>();

            string[] tables = Directory.GetFiles(System.Environment.CurrentDirectory + "/Loot/", "*.xml");

            foreach (string table in tables)
            {
                LootTable newTable = new LootTable(table);
                _lootTables.Add(newTable.botID, newTable);
            }
        }

        public void tryLootDrop(IEnumerable<Player> players, Bot dead, short posX, short posY)
        {
            if (players.Count() == 0)
                return;

            using (LogAssume.Assume(_logger))
            {
                LootTable lootTable = getLootTableByID(dead._type.Id);

                if (lootTable == null)
                {
                    Log.write("Could not find loot table for bot id {0}", dead._type.Id);
                    return;
                }

                int weightRoll = 0;
                //Spawn all of our normal items
                if (lootTable.normalLoot.Count > 0)
                {
                    foreach (LootInfo loot in lootTable.normalLoot)
                    {
                        if (loot._weight >= 100)
                        itemSpawn(loot._item, (ushort)loot._quantity, posX, posY, null);
                        else
                        {
                            weightRoll = _rand.Next(0, 100);
                            if (loot._weight >= weightRoll)
                                itemSpawn(loot._item, (ushort)loot._quantity, posX, posY, null);
                        }
                    }
                } 
                

                int total = 0;
                int position = 0;

                total += lootTable.commonChance;
                total += lootTable.uncommonChance;
                total += lootTable.setChance;
                total += lootTable.rareChance;

                Range commonRange = new Range(position, position + lootTable.commonChance);
                position += lootTable.commonChance;
                Range uncommonRange = new Range(position, position + lootTable.uncommonChance);
                position += lootTable.uncommonChance;
                Range setRange = new Range(position, position + lootTable.setChance);
                position += lootTable.setChance;
                Range rareRange = new Range(position, position + lootTable.rareChance);
                position += lootTable.rareChance;
                weightRoll = 0;
                List<LootInfo> potentialLoot = new List<LootInfo>();
                foreach (Player player in players)
                {
                    Log.write("Trying loot drop for {0}", player._alias);
                    weightRoll = _rand.Next(0, total);

                    Log.write("Category Weight Roll: {0}", weightRoll);

                    if (weightRoll.IsWithin(commonRange.min, commonRange.max))
                    {
                        Log.write("selected commmon loot list");
                        potentialLoot = lootTable.commonLoot;
                    }
                    else if (weightRoll.IsWithin(uncommonRange.min, uncommonRange.max))
                    {
                        Log.write("selected uncommon loot list");
                        potentialLoot = lootTable.uncommonLoot;
                    }
                    else if (weightRoll.IsWithin(setRange.min, setRange.max))
                    {
                        Log.write("selected set loot list");
                        potentialLoot = lootTable.setLoot;
                    }
                    else if (weightRoll.IsWithin(rareRange.min, rareRange.max))
                    {
                        Log.write("selected rare loot list");
                        potentialLoot = lootTable.rareLoot;
                    }
                    else
                    {
                        //No loot for this guy :(
                        if (potentialLoot.Count == 0)
                        {
                            Log.write("no loot list selected");
                            continue;
                        }

                    }



                    int totalLootProbability = 0;
                    int rangePosition = 0;
                    Dictionary<Range, LootInfo> lootRanges = new Dictionary<Range, LootInfo>();

                    foreach (LootInfo loot in potentialLoot)
                    {
                        totalLootProbability += loot._weight;
                        lootRanges.Add(new Range(rangePosition, rangePosition + loot._weight), loot);
                        rangePosition += loot._weight;
                    }

                    weightRoll = _rand.Next(0, totalLootProbability);
                    Log.write("Item Weight Roll: {0}", weightRoll);
                    LootInfo drop = lootRanges.FirstOrDefault(rng => weightRoll.IsWithin(rng.Key.min, rng.Key.max)).Value;

                    //Better luck next time!
                    if (drop == null)
                        continue;

                    //Likely a null item to simulate chance of no drop at all
                    if (drop._item == null)
                        continue;

                    //Give the lad his loot!
                    privateItemSpawn(drop._item, (ushort)drop._quantity, dead._state.positionX, dead._state.positionY, player, drop._type, dead, drop._name);

                }
            }
        }

        public void tryLootDrop(IEnumerable<Player> players, Player dead, short posX, short posY)
        {
            if (players.Count() == 0)
                return;

            using (LogAssume.Assume(_logger))
            {
                LootTable lootTable = getLootTableByID(0);

                if (lootTable == null)
                {
                    Log.write("Could not find player loot table");
                    return;
                }

                int weightRoll = 0;
                //Spawn all of our normal items
                if (lootTable.normalLoot.Count > 0)
                {
                    foreach (LootInfo loot in lootTable.normalLoot)
                    {
                        if (loot._weight >= 100)
                            itemSpawn(loot._item, (ushort)loot._quantity, posX, posY, null);
                        else
                        {
                            weightRoll = _rand.Next(0, 100);
                            if (loot._weight >= weightRoll)
                                itemSpawn(loot._item, (ushort)loot._quantity, posX, posY, null);
                        }
                    }
                }


                int total = 0;
                int position = 0;

                total += lootTable.commonChance;
                total += lootTable.uncommonChance;
                total += lootTable.setChance;
                total += lootTable.rareChance;

                Range commonRange = new Range(position, position + lootTable.commonChance);
                position += lootTable.commonChance;
                Range uncommonRange = new Range(position, position + lootTable.uncommonChance);
                position += lootTable.uncommonChance;
                Range setRange = new Range(position, position + lootTable.setChance);
                position += lootTable.setChance;
                Range rareRange = new Range(position, position + lootTable.rareChance);
                position += lootTable.rareChance;
                weightRoll = 0;
                List<LootInfo> potentialLoot = new List<LootInfo>();
                foreach (Player player in players)
                {
                    Log.write("Trying loot drop for {0}", player._alias);
                    weightRoll = _rand.Next(0, total);

                    Log.write("Category Weight Roll: {0}", weightRoll);

                    if (weightRoll.IsWithin(commonRange.min, commonRange.max))
                    {
                        Log.write("selected commmon loot list");
                        potentialLoot = lootTable.commonLoot;
                    }
                    else if (weightRoll.IsWithin(uncommonRange.min, uncommonRange.max))
                    {
                        Log.write("selected uncommon loot list");
                        potentialLoot = lootTable.uncommonLoot;
                    }
                    else if (weightRoll.IsWithin(setRange.min, setRange.max))
                    {
                        Log.write("selected set loot list");
                        potentialLoot = lootTable.setLoot;
                    }
                    else if (weightRoll.IsWithin(rareRange.min, rareRange.max))
                    {
                        Log.write("selected rare loot list");
                        potentialLoot = lootTable.rareLoot;
                    }
                    else
                    {
                        //No loot for this guy :(
                        if (potentialLoot.Count == 0)
                        {
                            Log.write("no loot list selected");
                            continue;
                        }

                    }



                    int totalLootProbability = 0;
                    int rangePosition = 0;
                    Dictionary<Range, LootInfo> lootRanges = new Dictionary<Range, LootInfo>();

                    foreach (LootInfo loot in potentialLoot)
                    {
                        totalLootProbability += loot._weight;
                        lootRanges.Add(new Range(rangePosition, rangePosition + loot._weight), loot);
                        rangePosition += loot._weight;
                    }

                    weightRoll = _rand.Next(0, totalLootProbability);
                    Log.write("Item Weight Roll: {0}", weightRoll);
                    LootInfo drop = lootRanges.FirstOrDefault(rng => weightRoll.IsWithin(rng.Key.min, rng.Key.max)).Value;

                    //Better luck next time!
                    if (drop == null)
                        continue;

                    //Likely a null item to simulate chance of no drop at all
                    if (drop._item == null)
                        continue;

                    //Give the lad his loot!
                    privateItemSpawn(drop._item, (ushort)drop._quantity, dead._state.positionX, dead._state.positionY, player, drop._type, dead, drop._name);

                }
            }
        }

        /// <summary>
        /// Creates an item drop at the specified location
        /// </summary>
        public void privateItemSpawn(ItemInfo item, ushort quantity, short positionX, short positionY, Player p, LootType type, Player dead, string name)
        {
            using (LogAssume.Assume(_logger))
            {
                Log.write("[DROP] - {0} dropped for {1}", item.name, p._alias);

                if (item == null)
                {
                    Log.write(TLog.Error, "Attempted to spawn invalid item.");
                    return;
                }

                if (quantity == 0)
                {
                    Log.write(TLog.Warning, "Attempted to spawn 0 of an item.");
                    return;
                }

                //Too many items?
                if (_arena._items.Count == Arena.maxItems)
                {
                    Log.write(TLog.Warning, "Item count full.");
                    return;
                }

                int blockedAttempts = 35;
                Arena.ItemDrop spawn = null;

                short pX;
                short pY;
                while (true)
                {
                    pX = positionX;
                    pY = positionY;
                    Helpers.randomPositionInArea(_arena, _arena._server._zoneConfig.arena.pruneDropRadius, ref pX, ref pY);
                    if (_arena.getTile(pX, pY).Blocked)
                    {
                        blockedAttempts--;
                        if (blockedAttempts <= 0)
                            //Consider the spawn to be blocked
                            return;
                        continue;
                    }

                    //We want to continue wrapping around the vehicleid limits
                    //looking for empty spots.
                    ushort ik;

                    for (ik = _arena._lastItemKey; ik <= Int16.MaxValue; ++ik)
                    {   //If we've reached the maximum, wrap around
                        if (ik == Int16.MaxValue)
                        {
                            ik = (ushort)ZoneServer.maxPlayers;
                            continue;
                        }

                        //Does such an item exist?
                        if (_arena._items.ContainsKey(ik))
                            continue;

                        //We have a space!
                        break;
                    }



                    _arena._lastItemKey = ik;

                    //Create our drop class		
                    Arena.ItemDrop id = new Arena.ItemDrop();

                    id.item = item;
                    id.id = ik;
                    id.quantity = (short)quantity;
                    id.positionX = pX;
                    id.positionY = pY;
                    id.relativeID = item.relativeID;
                    id.freq = p._team._id;

                    id.owner = p; //For bounty abuse upon pickup

                    //Add it to our private loot tracker
                    _script._privateLoot.Add(id.id, new LootDrop(id, p, Environment.TickCount, ik));

                    int expire = _arena.getTerrain(positionX, positionY).prizeExpire;
                    id.tickExpire = (expire > 0 ? (Environment.TickCount + (expire * 1000)) : 0);

                    //Add it to our list
                    _arena._items[ik] = id;
                    //Notify the player
                    Helpers.Object_ItemDrop(_arena.Players, id);


                    if (type > LootType.Common)
                    {
                        foreach (Player player in _arena.Players)
                        {
                            if (player == p)
                                player.sendMessage(1, String.Format("{0} just dropped {1} for you at {2}",
                                dead._alias, name, Helpers.posToLetterCoord(id.positionX, id.positionY)));
                            else
                                player.triggerMessage(5, 1000, String.Format("{0} just dropped {1} for {2} at {3}",
                                dead._alias, name, p._alias, Helpers.posToLetterCoord(id.positionX, id.positionY)));
                        }
                    }
                    break;
                }
            }
        }


        /// <summary>
        /// Creates an item drop at the specified location
        /// </summary>
        public void privateItemSpawn(ItemInfo item, ushort quantity, short positionX, short positionY, Player p, LootType type, Bot dead, string name)
        {
            using (LogAssume.Assume(_logger))
            {
                Log.write("[DROP] - {0} dropped for {1}", item.name, p._alias);

                if (item == null)
                {
                    Log.write(TLog.Error, "Attempted to spawn invalid item.");
                    return;
                }

                if (quantity == 0)
                {
                    Log.write(TLog.Warning, "Attempted to spawn 0 of an item.");
                    return;
                }

                //Too many items?
                if (_arena._items.Count == Arena.maxItems)
                {
                    Log.write(TLog.Warning, "Item count full.");
                    return;
                }

                int blockedAttempts = 35;
                Arena.ItemDrop spawn = null;

                short pX;
                short pY;
                while (true)
                {
                    pX = positionX;
                    pY = positionY;
                    Helpers.randomPositionInArea(_arena, _arena._server._zoneConfig.arena.pruneDropRadius, ref pX, ref pY);
                    if (_arena.getTile(pX, pY).Blocked)
                    {
                        blockedAttempts--;
                        if (blockedAttempts <= 0)
                            //Consider the spawn to be blocked
                            return;
                        continue;
                    }

                    //We want to continue wrapping around the vehicleid limits
                    //looking for empty spots.
                    ushort ik;

                    for (ik = _arena._lastItemKey; ik <= Int16.MaxValue; ++ik)
                    {   //If we've reached the maximum, wrap around
                        if (ik == Int16.MaxValue)
                        {
                            ik = (ushort)ZoneServer.maxPlayers;
                            continue;
                        }

                        //Does such an item exist?
                        if (_arena._items.ContainsKey(ik))
                            continue;

                        //We have a space!
                        break;
                    }

                    

                    _arena._lastItemKey = ik;

                    //Create our drop class		
                    Arena.ItemDrop id = new Arena.ItemDrop();

                    id.item = item;
                    id.id = ik;
                    id.quantity = (short)quantity;
                    id.positionX = pX;
                    id.positionY = pY;
                    id.relativeID = item.relativeID;
                    id.freq = p._team._id;

                    id.owner = p; //For bounty abuse upon pickup

                    //Add it to our private loot tracker
                    _script._privateLoot.Add(id.id, new LootDrop(id, p, Environment.TickCount, ik));

                    int expire = _arena.getTerrain(positionX, positionY).prizeExpire;
                    id.tickExpire = (expire > 0 ? (Environment.TickCount + (expire * 1000)) : 0);

                    //Add it to our list
                    _arena._items[ik] = id;
                    //Notify the player
                    Helpers.Object_ItemDrop(_arena.Players, id);


                    if (type > LootType.Common)
                    {
                        foreach (Player player in _arena.Players)
                        {
                            if (player == p)
                                player.sendMessage(1, String.Format("$A {0} just dropped {1} for you at {2}",
                                dead._type.Name, name, Helpers.posToLetterCoord(id.positionX, id.positionY)));
                            else
                                player.triggerMessage(5, 1000, String.Format("A {0} just dropped {1} for {2} at {3}",
                                dead._type.Name, name, p._alias, Helpers.posToLetterCoord(id.positionX, id.positionY)));
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Creates an item drop at the specified location
        /// </summary>
        public void itemSpawn(ItemInfo item, ushort quantity, short positionX, short positionY, Player p)
        {
            using (LogAssume.Assume(_logger))
            {
                if (item == null)
                {
                    Log.write(TLog.Error, "Attempted to spawn invalid item.");
                    return;
                }

                if (quantity == 0)
                {
                    Log.write(TLog.Warning, "Attempted to spawn 0 of an item.");
                    return;
                }

                //Too many items?
                if (_arena._items.Count == Arena.maxItems)
                {
                    Log.write(TLog.Warning, "Item count full.");
                    return;
                }

                int blockedAttempts = 35;
                short pX;
                short pY;
                while (true)
                {
                    pX = positionX;
                    pY = positionY;
                    Helpers.randomPositionInArea(_arena, _arena._server._zoneConfig.arena.pruneDropRadius, ref pX, ref pY);
                    if (_arena.getTile(pX, pY).Blocked)
                    {
                        blockedAttempts--;
                        if (blockedAttempts <= 0)
                            //Consider the spawn to be blocked
                            return;
                        continue;
                    }

                    //We want to continue wrapping around the vehicleid limits
                    //looking for empty spots.
                    ushort ik;

                    for (ik = _arena._lastItemKey; ik <= Int16.MaxValue; ++ik)
                    {   //If we've reached the maximum, wrap around
                        if (ik == Int16.MaxValue)
                        {
                            ik = (ushort)ZoneServer.maxPlayers;
                            continue;
                        }

                        //Does such an item exist?
                        if (_arena._items.ContainsKey(ik))
                            continue;

                        //We have a space!
                        break;
                    }

                    _arena._lastItemKey = ik;

                    //Create our drop class		
                    Arena.ItemDrop id = new Arena.ItemDrop();

                    id.item = item;
                    id.id = ik;
                    id.quantity = (short)quantity;
                    id.positionX = pX;
                    id.positionY = pY;
                    id.relativeID = item.relativeID;
                    id.freq = -1;

                    id.owner = null; //For bounty abuse upon pickup

                    int expire = _arena.getTerrain(positionX, positionY).prizeExpire;
                    id.tickExpire = (expire > 0 ? (Environment.TickCount + (expire * 1000)) : 0);

                    //Add it to our list
                    _arena._items[ik] = id;

                    //Notify the arena
                    Helpers.Object_ItemDrop(_arena.Players, id);
                    break;
                }
            }

        }


        /// <summary>
        /// Grabs a loot table with the specified ID if it exists
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public LootTable getLootTableByID(int id)
        {
            if (_lootTables.ContainsKey(id))
                return _lootTables[id];
            else
                return null;
        }

        private List<E> ShuffleList<E>(List<E> inputList)
        {
            List<E> randomList = new List<E>();

            Random r = new Random();
            int randomIndex = 0;
            while (inputList.Count > 0)
            {
                randomIndex = r.Next(0, inputList.Count); //Choose a random object in the list
                randomList.Add(inputList[randomIndex]); //add it to the new, random list
                inputList.RemoveAt(randomIndex); //remove to avoid duplicates
            }

            return randomList; //return the new random list
        }
    }
}
