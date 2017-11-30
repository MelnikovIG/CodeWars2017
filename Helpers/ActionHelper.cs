﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Custom;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class ActionHelper
    {
        public static void SelectGroup(Group group)
        {
            CheckActionExistanse();

            var move = GlobalHelper.Move;

            move.Action = ActionType.ClearAndSelect;
            move.Group = group.Id;

            GroupHelper.CurrentGroup = group;
            CommandsHelper.Commands.Add(new BaseCommand(CommandType.SelectGroup));
        }

        public static void Select(double left, double top, double right, double bottom, VehicleType? vehicleType = null)
        {
            CheckActionExistanse();

            var move = GlobalHelper.Move;

            move.Action = ActionType.ClearAndSelect;
            move.Top = top;
            move.Left = left;
            move.Right = right;
            move.Bottom = bottom;
            move.VehicleType = vehicleType;
            CommandsHelper.Commands.Add(new BaseCommand(CommandType.SelectGroup));
        }

        public static void StopMove()
        {
            CheckActionExistanse();

            GlobalHelper.Move.Action = ActionType.Move;
            GlobalHelper.Move.X = 0;
            GlobalHelper.Move.Y = 0;

            CommandsHelper.Commands.Add(new StopMoveGroupCommand());
        }

        public static void Move(double x, double y)
        {
            CheckActionExistanse();

            GlobalHelper.Move.Action = ActionType.Move;
            GlobalHelper.Move.X = x;
            GlobalHelper.Move.Y = y;

            CommandsHelper.Commands.Add(new MoveCommand());
        }

        public static void SetSelectedGroup(Group group)
        {
            CheckActionExistanse();

            GlobalHelper.Move.Action = ActionType.Assign;
            GlobalHelper.Move.Group = group.Id;

            GroupHelper.CurrentGroup = group;
            CommandsHelper.Commands.Add(new BaseCommand(CommandType.SetGroup));
        }

        public static void Scale(double x, double y, double factor)
        {
            CheckActionExistanse();

            GlobalHelper.Move.Action = ActionType.Scale;
            GlobalHelper.Move.X = x;
            GlobalHelper.Move.Y = y;
            GlobalHelper.Move.Factor = factor;

            CommandsHelper.Commands.Add(new BaseCommand(CommandType.Scale));
        }

        public static void NuclearStrike(long vehicleId, double x, double y)
        {
            CheckActionExistanse();

            GlobalHelper.Move.Action = ActionType.TacticalNuclearStrike;
            GlobalHelper.Move.X = x;
            GlobalHelper.Move.Y = y;
            GlobalHelper.Move.VehicleId = vehicleId;

            CommandsHelper.Commands.Add(new NuclearStrikeCommand(vehicleId));
        }

        public static void StartFactoryProduction(long facilityId, VehicleType vehicleType)
        {
            CheckActionExistanse();

            GlobalHelper.Move.Action = ActionType.SetupVehicleProduction;
            GlobalHelper.Move.FacilityId = facilityId;
            GlobalHelper.Move.VehicleType = vehicleType;

            CommandsHelper.Commands.Add(new BaseCommand(CommandType.StartFactoryProduction));
        }

        public static void StopFactoryProduction(long facilityId)
        {
            CheckActionExistanse();

            GlobalHelper.Move.Action = ActionType.SetupVehicleProduction;
            GlobalHelper.Move.FacilityId = facilityId;
            GlobalHelper.Move.VehicleType = null;

            CommandsHelper.Commands.Add(new BaseCommand(CommandType.StartFactoryProduction));
        }

        private static void CheckActionExistanse()
        {
            if (GlobalHelper.Move.Action != null)
            {
                throw new Exception("Действие уже задано на этом ходу");
            }
        }
    }
}
