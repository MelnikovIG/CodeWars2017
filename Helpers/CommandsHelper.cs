using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class CommandsHelper
    {
        public static List<BaseCommand> Commands { get; set; } = new List<BaseCommand>(500);
    }

    public class BaseCommand
    {
        public CommandType CommandType { get; }

        public BaseCommand(CommandType commandType)
        {
            CommandType = commandType;
        }
    }

    public class StopMoveGroupCommand : BaseCommand
    {
        public StopMoveGroupCommand() : base(CommandType.StopMove)
        {
        }
    }

    public class NuclearStrikeCommand : BaseCommand
    {
        public readonly long VehicleId;

        public NuclearStrikeCommand(long vehicleId) : base(CommandType.NuclearStrike)
        {
            VehicleId = vehicleId;
        }
    }

    public class MoveCommand : BaseCommand
    {
        public MoveCommand() : base(CommandType.Move)
        {
        }
    }

    public enum CommandType
    {
        Move,
        StopMove,
        NuclearStrike,
        Scale,
    }
}
