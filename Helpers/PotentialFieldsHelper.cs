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
        public const int BaseWorldCenterPower = 1;
        public const int PowerToNuclearStrike = -100;
        public const int EnemyPowerToDodge = 100;
        public const int AllyDodgePower = 50;

        public static int PpSize = 32;
        public static float[,] PotentialFields = new float[PpSize, PpSize];

        public static float[,] RangePowerMask5 = CreateSquareLinearPf(5);
        public static float[,] RangePowerMask49 = CreateSquareLinearPf(49); //Притягиваем на 2/3 карты

        public static void Clear()
        {
            PotentialFields = BaseWordPower.Clone() as float[,];
        }

        public static float[,] CreateSquareLinearPf(int range)
        {
            if (range % 2 == 0)
            {
                throw new Exception("Требуется нечетное число");
            }

            var result = new float[range, range];

            var centerIndex = range / 2;

            for(int i = 0; i < range; i++)
            {
                for (int j = 0; j < range; j++)
                {
                    var root = GetDistanceTo(i, j, centerIndex, centerIndex);
                    result[i, j] = (float)root;
                }
            }

            var maxPower = (from float x in result select x).Max();
            for (int i = 0; i < range; i++)
            {
                for (int j = 0; j < range; j++)
                {
                    result[i, j] = (maxPower - result[i, j])/maxPower;
                }
            }

            return result;
        }

        public static void ApplyPower(float[,] source, int sourceCenterX, int sourceCenterY, float[,] powerMask, float power)
        {
            var maskLength = (int)Math.Sqrt(powerMask.Length);
            var sourceLength = (int)Math.Sqrt(source.Length);

            var centerIndex = maskLength / 2;

            for (int i = 0; i < maskLength; i++)
            {
                for (int j = 0; j < maskLength; j++)
                {
                    var ri = (sourceCenterX - centerIndex) + i;
                    var rj = (sourceCenterY - centerIndex) + j;

                    if (ri < 0 || ri >= sourceLength || rj < 0 || rj >= sourceLength)
                    {
                        continue;
                    }

                    source[ri, rj] += power * powerMask[i, j];
                }
            }
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
                    var power = dist / maxDist * BaseWorldCenterPower;
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
            var enemies = UnitHelper.Units.Values.Where(x => x.Side == Side.Enemy).ToArray();

            enemies = enemies.Where(x => x.Type != VehicleType.Arrv).ToArray();

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

                            var power = (1 - sumDelta / maxSum) * PowerToNuclearStrike;
                            PotentialFields[i, j] += (float)power;
                        }
                    }
                }
            }
        }

        public static void AppendEnemyPowerToDodge()
        {
            var currentSelectedGroup = CommandsHelper.CurrentSelectedGroup;

            var enemyPower = EnemyPowerToDodge;

            var enemies = UnitHelper.Units.Values.Where(x => x.Side == Side.Enemy).ToArray();

            foreach (var enemy in enemies)
            {
                var power = enemyPower;

                if (currentSelectedGroup == (int) Groups.F1)
                {
                    if (enemy.Type == VehicleType.Helicopter)
                    {
                        power = -enemyPower;
                    }
                    else if (enemy.Type == VehicleType.Tank)
                    {
                        power = 0;
                    }
                    else if (enemy.Type == VehicleType.Arrv)
                    {
                        power = 0;
                    }
                }
                else if (currentSelectedGroup == (int)Groups.H1)
                {
                    if (enemy.Type == VehicleType.Tank)
                    {
                        power = -enemyPower;
                    }
                    if (enemy.Type == VehicleType.Arrv)
                    {
                        power = -enemyPower/2;
                    }
                }
                else if (currentSelectedGroup == (int)Groups.Tank1)
                {
                    if (enemy.Type == VehicleType.Ifv)
                    {
                        power = -enemyPower;
                    }
                    if (enemy.Type == VehicleType.Arrv)
                    {
                        power = -enemyPower / 2;
                    }
                    if (enemy.Type == VehicleType.Fighter)
                    {
                        power = 0;
                    }
                }
                else if (currentSelectedGroup == (int)Groups.Bmp1)
                {
                    if (enemy.Type == VehicleType.Helicopter)
                    {
                        power = -enemyPower;
                    }
                    if (enemy.Type == VehicleType.Fighter)
                    {
                        power = -enemyPower;
                    }
                    if (enemy.Type == VehicleType.Arrv)
                    {
                        power = -enemyPower / 2;
                    }
                }

                var cellX = (int)enemy.X / PpSize;
                var cellY = (int)enemy.Y / PpSize;

                if (power > 0)
                {
                    ApplyPower(PotentialFields, cellX, cellY, RangePowerMask5, power);
                }
                else
                {
                    ApplyPower(PotentialFields, cellX, cellY, RangePowerMask49, power);
                }
            }

            //Сделаем притиягивание к вертолетам или самолетам
            if (currentSelectedGroup == (int) Groups.Healer1)
            {
                var helicopters = UnitHelper.UnitsAlly.Where(x => x.Type == VehicleType.Helicopter).ToList();
                if (helicopters.Count > 0)
                {
                    var cx = helicopters.Sum(x => x.X) / helicopters.Count;
                    var cy = helicopters.Sum(x => x.Y) / helicopters.Count;

                    var cellX = (int) cx / PpSize;
                    var cellY = (int) cy / PpSize;

                    ApplyPower(PotentialFields, cellX, cellY, RangePowerMask49, -enemyPower * 0.5f);
                }

                var fighters = UnitHelper.UnitsAlly.Where(x => x.Type == VehicleType.Fighter).ToList();
                if (fighters.Count > 0)
                {
                    var cx = fighters.Sum(x => x.X) / fighters.Count;
                    var cy = fighters.Sum(x => x.Y) / fighters.Count;

                    var cellX = (int)cx / PpSize;
                    var cellY = (int)cy / PpSize;

                    ApplyPower(PotentialFields, cellX, cellY, RangePowerMask49, -enemyPower * 0.5f);
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

        public static void AppendAllyUnitsToDodge(MyLivingUnit[] selectedUnits)
        {
            var currentSelectedGroup = CommandsHelper.CurrentSelectedGroup;

            MyLivingUnit[] allyUnitsToDodge = new MyLivingUnit[0];

            if (currentSelectedGroup == (int) Groups.F1 || currentSelectedGroup == (int) Groups.H1)
            {
                var selectedUnitIds = selectedUnits.Select(x => x.Id).ToArray();
                allyUnitsToDodge = UnitHelper.Units.Values
                    .Where(
                        x => x.Side == Side.Our
                             && (x.Type == VehicleType.Fighter || x.Type == VehicleType.Helicopter)
                             && !selectedUnitIds.Contains(x.Id)).ToArray();
            }
            else if (currentSelectedGroup == (int) Groups.Tank1 || currentSelectedGroup == (int) Groups.Bmp1 || currentSelectedGroup == (int)Groups.Healer1)
            {
                var selectedUnitIds = selectedUnits.Select(x => x.Id).ToArray();
                allyUnitsToDodge = UnitHelper.Units.Values
                    .Where(
                        x => x.Side == Side.Our
                             && (x.Type == VehicleType.Tank || x.Type == VehicleType.Ifv || x.Type == VehicleType.Arrv)
                             && !selectedUnitIds.Contains(x.Id)).ToArray();
            }

            foreach (var allyDodgeUnit in allyUnitsToDodge)
            {
                var cellX = (int)allyDodgeUnit.X / PpSize;
                var cellY = (int)allyDodgeUnit.Y / PpSize;

                ApplyPower(PotentialFields, cellX, cellY, RangePowerMask5, AllyDodgePower);
            }
        }
    }
}
