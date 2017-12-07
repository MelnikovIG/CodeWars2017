using System;
using System.Linq;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class NuclearStrikeHelper
    {
        public static NuclearStrikeState NuclearStrikeState { get; set; } = NuclearStrikeState.None;
        public static Player Enemy => GlobalHelper.Enemy;
        public static bool IsEnemyNuclearStrikeExecuting => Enemy.NextNuclearStrikeTickIndex >= 0;
        public static double EnemyNuclearStrikeX => Enemy.NextNuclearStrikeX;
        public static double EnemyNuclearStrikeY => Enemy.NextNuclearStrikeY;

        public static bool ProcessNuclearStrike(bool moveAllowed)
        {
            var isEmenyExecutingNs = IsEnemyNuclearStrikeExecuting;

            if (isEmenyExecutingNs)
            {
                switch (NuclearStrikeState)
                {
                    case NuclearStrikeState.None:
                        var anyUnitsInRangeOfNuclearStrike = CheckAnyUnitsInRangeOfNuclearStrike();
                        if (anyUnitsInRangeOfNuclearStrike)
                        {
                            MakeSpread(moveAllowed);
                            NuclearStrikeState = NuclearStrikeState.Spread;
                            return true;
                        }
                        break;
                    case NuclearStrikeState.Spread:
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
                        break;
                    case NuclearStrikeState.Spread:
                        MakeGather(moveAllowed);
                        NuclearStrikeState = NuclearStrikeState.Gather;
                        return true;
                        break;
                    case NuclearStrikeState.Gather:
                        NuclearStrikeState = NuclearStrikeState.None;
                        break;
                    default: throw new Exception();
                }
            }

            return false;
        }

        private static bool CheckAnyUnitsInRangeOfNuclearStrike()
        {
            var nsRadius = GlobalHelper.Game.TacticalNuclearStrikeRadius;

            var allyUnitsInEnemyNs = UnitHelper.UnitsAlly
                .Where(x => 
                GeometryHelper.PointIsWithinCircle(EnemyNuclearStrikeX, EnemyNuclearStrikeY, nsRadius, x.X, x.Y))
                .Any();

            return allyUnitsInEnemyNs;
        }

        private static void MakeGather(bool moveAllowed)
        {
            var nsRadius = GlobalHelper.Game.TacticalNuclearStrikeRadius;

            if (moveAllowed)
            {
                ActionHelper.Select(
                    EnemyNuclearStrikeX - nsRadius,
                    EnemyNuclearStrikeY - nsRadius,
                    EnemyNuclearStrikeX + nsRadius,
                    EnemyNuclearStrikeY + nsRadius);
            }
            else
            {
                QueueHelper.Queue.Enqueue(new SelectUnits(
                    EnemyNuclearStrikeX - nsRadius,
                    EnemyNuclearStrikeY - nsRadius,
                    EnemyNuclearStrikeX + nsRadius,
                    EnemyNuclearStrikeY + nsRadius));
            }

            QueueHelper.Queue.Enqueue(new Scale(EnemyNuclearStrikeX, EnemyNuclearStrikeY, 0.1));
        }

        private static void MakeSpread(bool moveAllowed)
        {
            if (moveAllowed)
            {
                ActionHelper.Scale(EnemyNuclearStrikeX, EnemyNuclearStrikeY, 10);
            }
            else
            {
                QueueHelper.Queue.Enqueue(new Scale(EnemyNuclearStrikeX, EnemyNuclearStrikeY, 10));
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
