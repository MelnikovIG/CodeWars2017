﻿using System;
using System.Linq;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class NuclearStrikeHelper
    {
        private static MyLivingUnit[] allyUnitsInRangeOfNuclearStrike;
        public static NuclearStrikeState NuclearStrikeState { get; set; } = NuclearStrikeState.None;
        public static Player Enemy => GlobalHelper.Enemy;
        public static bool IsEnemyNuclearStrikeExecuting => Enemy.NextNuclearStrikeTickIndex >= 0;

        private static double LastEnemyNuclearStrikeX { get; set; }
        private static double LastEnemyNuclearStrikeY { get; set; }

        public static bool ProcessNuclearStrike(bool moveAllowed)
        {
            var isEmenyExecutingNs = IsEnemyNuclearStrikeExecuting;

            if (isEmenyExecutingNs)
            {

                LastEnemyNuclearStrikeX = Enemy.NextNuclearStrikeX;
                LastEnemyNuclearStrikeY = Enemy.NextNuclearStrikeY;

                switch (NuclearStrikeState)
                {
                    case NuclearStrikeState.None:
                        allyUnitsInRangeOfNuclearStrike = GetAllyUnitsInRangeOfNuclearStrike();
                        if (allyUnitsInRangeOfNuclearStrike.Length > 0)
                        {
                            MakeSpread(moveAllowed);
                            NuclearStrikeState = NuclearStrikeState.Spread;
                            return true;
                        }
                        break;
                    case NuclearStrikeState.Spread:
                        return true;
                        break;
                    case NuclearStrikeState.Gather:
                        throw new Exception();
                        break;
                    default: throw new Exception();
                }
            }
            else
            {
                switch (NuclearStrikeState)
                {
                    case NuclearStrikeState.None:
                        return false;
                        break;
                    case NuclearStrikeState.Spread:
                        MakeGather(moveAllowed);
                        NuclearStrikeState = NuclearStrikeState.Gather;
                        return true;
                        break;
                    case NuclearStrikeState.Gather:
                        NuclearStrikeState = NuclearStrikeState.None;
                        return false;
                        break;
                    default: throw new Exception();
                }

            }

            return false;
        }

        private static MyLivingUnit[] GetAllyUnitsInRangeOfNuclearStrike()
        {
            var nsRadius = GlobalHelper.Game.TacticalNuclearStrikeRadius;

            var allyUnitsInEnemyNs = UnitHelper.UnitsAlly
                .Where(x => 
                GeometryHelper.PointIsWithinCircle(LastEnemyNuclearStrikeX, LastEnemyNuclearStrikeY, nsRadius, x.X, x.Y))
                .ToArray();

            return allyUnitsInEnemyNs;
        }

        private static void MakeSpread(bool moveAllowed)
        {
            var nsRadius = GlobalHelper.Game.TacticalNuclearStrikeRadius;

            if (moveAllowed)
            {
                ActionHelper.Select(
                    LastEnemyNuclearStrikeX - nsRadius,
                    LastEnemyNuclearStrikeY - nsRadius,
                    LastEnemyNuclearStrikeX + nsRadius,
                    LastEnemyNuclearStrikeY + nsRadius
                );
                QueueHelper.Queue.Enqueue(new Scale(LastEnemyNuclearStrikeX, LastEnemyNuclearStrikeY, 10));
            }
            else
            {
                QueueHelper.Queue.Enqueue(new SelectUnits(
                    LastEnemyNuclearStrikeX - nsRadius,
                    LastEnemyNuclearStrikeY - nsRadius,
                    LastEnemyNuclearStrikeX + nsRadius,
                    LastEnemyNuclearStrikeY + nsRadius
                    ));
                QueueHelper.Queue.Enqueue(new Scale(LastEnemyNuclearStrikeX, LastEnemyNuclearStrikeY, 10));
            }
        }

        private static void MakeGather(bool moveAllowed)
        {
            //TODO: useMoveAllowed 

            var groups = allyUnitsInRangeOfNuclearStrike
                .Where(x => x.Groups.Length > 0)
                .GroupBy(x => x.Groups[0])
                .ToArray();

            foreach (var group in groups)
            {
                var chosenGroup = GroupHelper.Groups[group.Key - 1];

                //var isNucleatorInGroup = group.Select(x => x.Id).Contains(GlobalHelper.Me.NextNuclearStrikeVehicleId);
                //if (isNucleatorInGroup)
                //{
                //    continue;
                //}

                if (GroupHelper.CurrentGroup != chosenGroup)
                {
                    QueueHelper.Queue.Enqueue(new SelectGroup(chosenGroup));

                    for (int i = 0; i < 7; i++)
                    {
                        QueueHelper.Queue.Enqueue(new Scale(LastEnemyNuclearStrikeX, LastEnemyNuclearStrikeY, 0.1));
                    }
                }
            }
        }
    }

    public enum NuclearStrikeState
    {
        None,
        Spread,
        Gather
    }
}
