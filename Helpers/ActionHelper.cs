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
        public static void Select(double top, double left, double right, double bottom, VehicleType? vehicleType = null)
        {
            var move = GlobalHelper.Move;

            move.Action = ActionType.ClearAndSelect;
            move.Top = top;
            move.Left = left;
            move.Right = right;
            move.Bottom = bottom;
            move.VehicleType = vehicleType;
        }

        public static void Move(double x, double y)
        {
            GlobalHelper.Move.Action = ActionType.Move;
            GlobalHelper.Move.X = x;
            GlobalHelper.Move.Y = y;
        }
    }
}
