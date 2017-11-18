using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Custom;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class PotentialFieldsHelper
    {
        public static int PpSize = 32;
        public static float[,] PotentialFields = new float[PpSize, PpSize];

        public static void Clear()
        {
            PotentialFields = BaseWordPower.Clone() as float[,];
        }

        public static float[,] BaseWordPower = new float[PpSize, PpSize];

        static PotentialFieldsHelper()
        {
            CreateBaseWorldPower();
        }

        static void CreateBaseWorldPower()
        {
            double xRange = PpSize / 2;
            double yRange = PpSize / 2;
            var maxDist = Math.Sqrt(xRange * xRange + yRange * yRange);

            for (int i = 0; i < PpSize; i++)
            {
                for (int j = 0; j < PpSize; j++)
                {
                    var dist = GetDistanceTo(PpSize / 2, PpSize / 2, i, j);
                    var power = dist / maxDist * 1;
                    BaseWordPower[i, j] = (float)power;
                }
            }
        }

        public static double GetDistanceTo(double x1, double y1, double x2, double y2)
        {
            double xRange = x2 - x1;
            double yRange = y2 - y1;
            return Math.Sqrt(xRange * xRange + yRange * yRange);
        }

        //NOTE: кривой алгоритм расчета ПП
        public static void ApplyPowerToNuclearStrike()
        {
            var enemyPower = -10;

            var enemies = UnitHelper.Units.Values.Where(x => x.Side == Side.Enemy).ToArray();
            for (int i = 0; i < PpSize; i++)
            {
                for (int j = 0; j < PpSize; j++)
                {
                    foreach (var enemy in enemies.Take(1))
                    {
                        var dx = Math.Abs(enemy.X / PpSize - i);
                        var dy = Math.Abs(enemy.Y / PpSize - j);

                        var sumDelta = dx + dy;
                        if (sumDelta < 32)
                        {
                            var maxSum = PpSize * 2;

                            var power = (1 - sumDelta / maxSum) * enemyPower;
                            PotentialFields[i, j] += (float)power;
                        }
                    }
                }
            }
        }

        public static void AppendEnemyPower()
        {
            var enemyPower = 100;

            var enemies = UnitHelper.Units.Values.Where(x => x.Side == Side.Enemy).ToArray();
            foreach(var enemy in enemies)
            {
                var cellX = (int)enemy.X / PpSize;
                var cellY = (int)enemy.Y / PpSize;

                PotentialFields[cellX, cellY] += enemyPower;

                if (cellX - 1 > 0)
                {
                    PotentialFields[cellX - 1, cellY] += enemyPower;
                }

                if (cellX + 1 < PpSize)
                {
                    PotentialFields[cellX + 1, cellY] += enemyPower;
                }

                if (cellY - 1 > 0)
                {
                    PotentialFields[cellX, cellY - 1] += enemyPower;
                }

                if (cellY + 1 < PpSize)
                {
                    PotentialFields[cellX, cellY + 1] += enemyPower;
                }
            }
        }

        public static void Normalize()
        {
            var minPower = (from float x in PotentialFields select x).Min();
            if (minPower < 0)
            {
                var poweToAdd = -minPower;
                for (int i = 0; i < PpSize; i++)
                {
                    for (int j = 0; j < PpSize; j++)
                    {
                        PotentialFields[i, j] += poweToAdd;
                    }
                }
            }

            var maxPower = (from float x in PotentialFields select x).Max();

            for (int i = 0; i < PpSize; i++)
            {
                for (int j = 0; j < PpSize; j++)
                {
                    PotentialFields[i, j] = PotentialFields[i, j] / maxPower;
                }
            }
        }

        public static Color GetColorFromValue(float fieldValue)
        {
            return Color.FromArgb(100, (int)(fieldValue * 255), (int)(255 - fieldValue * 255), 0);
        }

        public static Point2D GetNextSafest_PP_PointByWorldXY(double cx, double cy)
        {
            var cellX = (int)cx / PpSize;
            var cellY = (int)cy / PpSize;

            var aroundPoints = GetPoinsAround(cellX, cellY);
            var minAroudPointsValue = aroundPoints.Select(x => PotentialFields[(int)x.X, (int)x.Y]).Min();
            var nextPoint = aroundPoints.FirstOrDefault(x => PotentialFields[(int)x.X, (int)x.Y] == minAroudPointsValue);
            return nextPoint;
        }

        private static List<Point2D> GetPoinsAround(double x, double y)
        {
            var result = new List<Point2D>(9);

            if (x > 0 && y > 0)
            {
                result.Add(new Point2D(x - 1, y - 1));
            }

            if (y > 0)
            {
                result.Add(new Point2D(x, y - 1));
            }

            if (x < PpSize - 1 && y > 0)
            {
                result.Add(new Point2D(x + 1, y - 1));
            }

            if (x < PpSize - 1)
            {
                result.Add(new Point2D(x + 1, y));
            }

            if (x < PpSize - 1 && y < PpSize - 1)
            {
                result.Add(new Point2D(x + 1, y + 1));
            }

            if (y < PpSize - 1)
            {
                result.Add(new Point2D(x, y + 1));
            }

            if (x > 0 && y < PpSize - 1)
            {
                result.Add(new Point2D(x - 1, y + 1));
            }

            if (x > 0)
            {
                result.Add(new Point2D(x - 1, y));
            }

            return result;
        }

        public static void AppendAllyFlyingPowerForFlyingUnits(MyLivingUnit[] selectedUnits)
        {
            var selectedUnitIds = selectedUnits.Select(x => x.Id).ToArray();
            var otherAllyFliers = UnitHelper.Units.Values
                .Where(
                x => x.Side == Side.Our
                && (x.Type == VehicleType.Fighter || x.Type == VehicleType.Helicopter)
                && !selectedUnitIds.Contains(x.Id)).ToArray();

            var allyFlierPower = 50;
            foreach (var otherAllyFlier in otherAllyFliers)
            {
                var cellX = (int)otherAllyFlier.X / PpSize;
                var cellY = (int)otherAllyFlier.Y / PpSize;

                PotentialFields[cellX, cellY] += allyFlierPower;

                if (cellX - 1 > 0)
                {
                    PotentialFields[cellX - 1, cellY] += allyFlierPower;
                }

                if (cellX + 1 < PpSize)
                {
                    PotentialFields[cellX + 1, cellY] += allyFlierPower;
                }

                if (cellY - 1 > 0)
                {
                    PotentialFields[cellX, cellY - 1] += allyFlierPower;
                }

                if (cellY + 1 < PpSize)
                {
                    PotentialFields[cellX, cellY + 1] += allyFlierPower;
                }
            }
        }
    }
}
