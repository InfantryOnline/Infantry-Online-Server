using System;
using System.Collections.Generic;
using System.Linq;
using InfServer.Game;

namespace InfServer.Logic.Events
{
    /// <summary>
    /// Takes care of creating events and actions.
    /// </summary>
    public static class EventsActionsFactory
    {
        public static GameEvent CreateGameEventFromString(string actions)
        {
            var output = new GameEvent();
            actions = actions.ToLower();

            foreach (var action in actions.Split(','))
            {
                var act = action;
                var args = String.Empty;

                if (action.Contains('='))
                {
                    var parms = action.Split('=');

                    act = parms[0];
                    args = parms[1];
                }

                output.AddAction(CreateActionFromString(act, args));
            }

            return output;
        }

        public static IAction CreateActionFromString(string action, string args)
        {
            IAction output;

            switch (action)
            {
                case "setexp":
                    output = new SetExperienceAction(Convert.ToInt32(args));
                    break;

                case "addexp":
                    output = new AddExperienceAction(Convert.ToInt32(args));
                    break;

                case "setcash":
                    output = new SetCashAction(Convert.ToInt32(args));
                    break;

                case "addcash":
                    output = new AddCashAction(Convert.ToInt32(args));
                    break;

                case "setbounty":
                    output = new SetBountyAction(Convert.ToInt32(args));
                    break;

                case "addbounty":
                    output = new AddBountyAction(Convert.ToInt32(args));
                    break;

                case "addinv":
                    var fields = args.Split(':');

                    output = fields.Length == 1 ? new AddInventoryAction(fields[0]) : new AddInventoryAction(fields[0], Convert.ToInt32(fields[1]));
                    break;

                case "addskill":
                    output = new AddSkillAction(args);
                    break;

                case "warp":
                    var warpGroup = 0;
                    Int32.TryParse(args, out warpGroup);

                    output = new WarpAction(warpGroup);
                    break;

                // Events

                case "vehicleevent":
                case "vehicleevent1":
                    output = new CallEventAction(p => CreateGameEventFromString(p._baseVehicle._type.EventString1));
                    break;

                case "vehicleevent2":
                    output = new CallEventAction(p => CreateGameEventFromString(p._baseVehicle._type.EventString2));
                    break;

                case "vehicleevent3":
                    output = new CallEventAction(p => CreateGameEventFromString(p._baseVehicle._type.EventString3));
                    break;

                case "teamevent":
                    output = new CallEventAction(p => CreateGameEventFromString(p._team._info.eventString));
                    break;

                case "terrainevent":
                    output = new CallEventAction(p => CreateGameEventFromString(p._arena.getTerrain(p._state.positionX, p._state.positionY).eventString));
                    break;

                default:
                    throw new ArgumentException("Provided event action cannot be handled: {0}", action);
            }

            return output;
        }
    }

    /// <summary>
    /// An event is a small game script that is executed on a particular player.
    /// </summary>
    public class GameEvent
    {
        public GameEvent()
        {
            Actions = new List<IAction>();
        }

        public GameEvent(List<IAction> actions)
        {
            Actions = actions;
        }

        public void AddAction(IAction action)
        {
            Actions.Add(action);
        }

        public virtual void Execute(Player p)
        {
            foreach (var a in Actions)
            {
                a.Execute(p);
            }
        }

        protected List<IAction> Actions;
    }

    /// <summary>
    /// An action is a piece of data that an Event can execute on a player.
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// Run this action for a player.
        /// </summary>
        /// <param name="p"></param>
        /// <returns>true if player state has been modified; false otherwise</returns>
        bool Execute(Player p);
    }

    /// <summary>
    /// Adds an inventory item to a player.
    /// </summary>
    public class AddInventoryAction : IAction
    {
        public string Name { get; private set; }

        public int Amount { get; private set; }

        public AddInventoryAction(string name, int amount)
        {
            Name = name;
            Amount = amount;
        }

        public AddInventoryAction(string name)
        {
            Name = name;
            Amount = 1;
        }

        public bool Execute(Player p)
        {
            var item = p._server._assets.getItemByName(Name);

            if (item == null)
            {
                return false;
            }

            p.inventoryModify(false, item, Amount);
            p.syncState();

            return true;
        }
    }

    /// <summary>
    /// Removes items from a player.
    /// </summary>
    public class WipeInventoryAction : IAction
    {
        public string Name { get; private set; }

        public int? Id { get; private set; }

        public bool WipeAllInventory { get { return String.IsNullOrWhiteSpace(Name) && Id == null; } }

        public WipeInventoryAction(string name)
        {
            Name = name;
        }

        public WipeInventoryAction(int? id)
        {
            Id = id;
        }

        public bool Execute(Player p)
        {
            if (WipeAllInventory)
            {
                p._inventory.Clear();
                p.syncInventory();
            }
            else
            {
                if (!String.IsNullOrWhiteSpace(Name))
                {
                    var item = p._server._assets.getItemByName(Name);

                    if (item == null)
                    {
                        return false;
                    }

                    Id = item.id;
                }

                if (!Id.HasValue)
                {
                    return false;
                }

                p.removeAllItemFromInventory(true, Id.Value);
            }

            return true;
        }
    }

    /// <summary>
    /// Adds a skill to a player.
    /// </summary>
    public class AddSkillAction : IAction
    {
        public string SkillName { get; private set; }

        public AddSkillAction(String skillName)
        {
            SkillName = skillName;
        }

        public bool Execute(Player p)
        {
            var skill = p._server._assets.getSkillByName(SkillName);

            if (skill == null)
            {
                return false;
            }

            p.skillModify(false, skill, 0);
            p.syncState();

            return true;
        }
    }

    /// <summary>
    /// Removes skills from a player.
    /// </summary>
    public class WipeSkillAction : IAction
    {
        public string SkillName { get; private set; }

        public bool WipeAllSkills { get { return String.IsNullOrWhiteSpace(SkillName); } }

        public WipeSkillAction(string skillName)
        {
            SkillName = skillName;
        }

        public bool Execute(Player p)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Updates the player's experience to a new value.
    /// </summary>
    public class SetExperienceAction : IAction
    {
        public int Amount { get; private set; }

        public SetExperienceAction(int amount)
        {
            Amount = amount;
        }

        public bool Execute(Player p)
        {
            p.Experience = Amount;
            p.syncState();

            return true;
        }
    }

    /// <summary>
    /// Adds experience to a player.
    /// </summary>
    public class AddExperienceAction : IAction
    {
        public int Amount { get; private set; }

        public AddExperienceAction(int amount)
        {
            Amount = amount;
        }

        public bool Execute(Player p)
        {
            p.Experience += Amount;
            p.syncState();

            return true;
        }
    }

    /// <summary>
    /// Updates the player's cash to a new value.
    /// </summary>
    public class SetCashAction : IAction
    {
        public int Amount { get; private set; }

        public SetCashAction(int amount)
        {
            Amount = amount;
        }

        public bool Execute(Player p)
        {
            p.Cash = Amount;
            p.syncState();

            return true;
        }
    }

    /// <summary>
    /// Adds cash to a player.
    /// </summary>
    public class AddCashAction : IAction
    {
        public int Amount { get; private set; }

        public AddCashAction(int amount)
        {
            Amount = amount;
        }

        public bool Execute(Player p)
        {
            p.Cash += Amount;
            p.syncState();

            return true;
        }
    }

    /// <summary>
    /// Updates a player's bounty to a new value.
    /// </summary>
    public class SetBountyAction : IAction
    {
        public int Amount { get; private set; }

        public SetBountyAction(int amount)
        {
            Amount = amount;
        }

        public bool Execute(Player p)
        {
            p.Bounty = Amount;
            p.syncState();

            return true;
        }
    }

    /// <summary>
    /// Adds bounty to a player.
    /// </summary>
    public class AddBountyAction : IAction
    {
        public int Amount { get; private set; }

        public AddBountyAction(int amount)
        {
            Amount = amount;
        }

        public bool Execute(Player p)
        {
            p.Bounty += Amount;
            p.syncState();

            return true;
        }
    }

    /// <summary>
    /// Teleports a player to a designated area.
    /// </summary>
    public class WarpAction : IAction
    {
        public int WarpGroupId { get; private set; }

        public WarpAction(int warpGroupId)
        {
            WarpGroupId = warpGroupId;
        }

        public bool Execute(Player p)
        {
            if (p._arena == null)
            {
                return false;
            }

            var warpGroups = p._server._assets.Lios.getWarpGroupByID(WarpGroupId);

            if (warpGroups == null)
            {
                return false;
            }

            // Write to a state object here!

            return false;
        }
    }

    public class ResetAction : IAction
    {
        public bool Execute(Player player)
        {
            if (player._arena == null)
            {
                return false;
            }



            return false;
        }
    }

    /// <summary>
    /// An action that executes another event.
    /// </summary>
    public class CallEventAction : IAction
    {
        public Func<Player, GameEvent> CalledEvent { get; private set; }

        public CallEventAction(Func<Player, GameEvent> calledEvent)
        {
            CalledEvent = calledEvent;
        }

        public bool Execute(Player p)
        {
            CalledEvent(p).Execute(p);

            return false;
        }
    }
}
