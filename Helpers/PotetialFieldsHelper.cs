using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Custom;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class PotetialFieldsHelper
    {
        public static int PpSize = 32;
        public static float[,] PotentialFields = new float[PpSize, PpSize];

        public static void Clear()
        {
            PotentialFields = new float[PpSize, PpSize];
        }

        public static void FillBaseWorldPower()
        {
            for (int i = 0; i < PpSize; i++)
            {
                for (int j = 0; j < PpSize; j++)
                {
                    var val = (i / (float)PpSize + j / (float)PpSize) / 2;
                    PotentialFields[i, j] = (1 - val) * 10000;
                }
            }
        }

        public static void AppendEnemyPower()
        {
            var enemies = UnitHelper.Units.Values.Where(x => x.Side == Side.Enemy).ToArray();
            foreach(var enemy in enemies)
            {
                var cellX = (int)enemy.X / PpSize;
                var cellY = (int)enemy.Y / PpSize;

                PotentialFields[cellX, cellY] += 100;

                if (cellX - 1 > 0)
                {
                    PotentialFields[cellX - 1, cellY] += 100;
                }

                if (cellX + 1 < PpSize)
                {
                    PotentialFields[cellX + 1, cellY] += 100;
                }

                if (cellY - 1 > 0)
                {
                    PotentialFields[cellX, cellY - 1] += 100;
                }

                if (cellY + 1 < PpSize)
                {
                    PotentialFields[cellX, cellY + 1] += 100;
                }
            }
        }

        public static void Normalize()
        {
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

            if (x < PpSize && y > 0)
            {
                result.Add(new Point2D(x + 1, y - 1));
            }

            if (x < PpSize)
            {
                result.Add(new Point2D(x + 1, y));
            }

            if (x < PpSize && y < PpSize)
            {
                result.Add(new Point2D(x + 1, y + 1));
            }

            if (y < PpSize)
            {
                result.Add(new Point2D(x, y + 1));
            }

            if (x > 0 && y < PpSize)
            {
                result.Add(new Point2D(x - 1, y + 1));
            }

            if (x > 0)
            {
                result.Add(new Point2D(x - 1, y));
            }

            return result;
        }
    }
}
