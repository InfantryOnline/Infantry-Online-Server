using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Multi
{
    public class Structure
    {
        private RTS _game;
        public string _key;
        public ushort _id;
        public VehInfo _type;
        public Helpers.ObjectState _state;
        public RTS.ProductionBuilding _buildingType;

        //Production
        public DateTime _nextProduction;
        public ItemInfo _productionItem;
        public int _productionQuantity;
        public int _productionLevel;
        public int _upgradeCost;


        public Vehicle _vehicle;

        public Structure(RTS game)
        {
            _game = game;
        }

        public void initState(Vehicle vehicle)
        {
            switch (vehicle._type.Id)
            {
                case 415:
                    _buildingType = RTS.ProductionBuilding.Shack;
                    break;
                case 424:
                    _buildingType = RTS.ProductionBuilding.House;
                    break;
                case 425:
                    _buildingType = RTS.ProductionBuilding.Villa;
                    break;
                case 427:
                    _buildingType = RTS.ProductionBuilding.Ironmine;
                    break;
                case 428:
                    _buildingType = RTS.ProductionBuilding.Scrapyard;
                    break;
            }
        }

        public void produce(Player player)
        {
            int level = _productionLevel;
            int cash = _game.calculateProduction(level, _buildingType);
            int cashNextLevel = _game.calculateProduction(level + 1, _buildingType);

            switch (_buildingType)
            {
                case RTS.ProductionBuilding.Shack:
                    {
                        player.Cash += cash;
                        player.syncState();


                        player.sendMessage(0, "&Production Collected");

                        //Update our next production.
                        _nextProduction = DateTime.Now.AddHours(RTS.c_shackProductionInterval);
                        _productionQuantity = cash;

                    }
                    break;
                case RTS.ProductionBuilding.House:
                    {
                        player.Cash += cash;
                        player.syncState();


                        player.sendMessage(0, "&Production Collected");

                        //Update our next production.
                        _nextProduction = DateTime.Now.AddHours(RTS.c_houseProductionInterval);
                        _productionQuantity = cash;

                    }
                    break;
                case RTS.ProductionBuilding.Villa:
                    {
                        player.Cash += cash;
                        player.syncState();


                        player.sendMessage(0, "&Production Collected");

                        //Update our next production.
                        _nextProduction = DateTime.Now.AddHours(RTS.c_villaProductionInterval);
                        _productionQuantity = cash;

                    }
                    break;
                case RTS.ProductionBuilding.Ironmine:
                    {
                        player.inventoryModify(_productionItem.id, cash);
                        player.syncState();

                        player.sendMessage(0, "&Production Collected");

                        //Update our next production.
                        _nextProduction = DateTime.Now.AddHours(RTS.c_baseIronProductionInterval);
                        _productionQuantity = cash;

                    }
                    break;
                case RTS.ProductionBuilding.Scrapyard:
                    {

                        player.inventoryModify(_productionItem.id, cash);
                        player.syncState();


                        player.sendMessage(0, "&Production Collected");

                        //Update our next production.
                        _nextProduction = DateTime.Now.AddHours(RTS.c_baseScrapProductionInterval);
                        _productionQuantity = cash;

                    }
                    break;
            }
            _game._database.updateStructure(this, _game._fileName);
        }

        public void destroyed(Vehicle dead, Player killer)
        {

            //Drop some cash if thats what we produce
            if (_productionQuantity != 0 && Convert.ToInt32(_buildingType) < 4)
            {
                double cashAmount = 0;
                ItemInfo cashItem = AssetManager.Manager.getItemByID(322);

                if (_productionQuantity >= 100 && _productionQuantity <= 1000)
                    cashAmount = Math.Round((double)_productionQuantity / 100);
                else if (_productionQuantity >= 1000 && _productionQuantity <= 10000)
                {
                    cashAmount = Math.Round((double)_productionQuantity / 1000);
                    cashItem = AssetManager.Manager.getItemByID(323);
                }
                else if (_productionQuantity >= 10000)
                {
                    cashAmount = Math.Round((double)_productionQuantity / 10000);
                    cashItem = AssetManager.Manager.getItemByID(324);
                }

                dead._arena.spawnItemInArea(cashItem, (ushort)cashAmount, dead._state.positionX, dead._state.positionY, 150);
            }

            //Update our database
            _game._database.removeStructure(_id, _game._fileName);

            //Remove it
            _game._structures.Remove(_id);
        }
    }
}
