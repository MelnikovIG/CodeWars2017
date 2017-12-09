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
        public const int HealPower = 10;
        public const float FliersToHealDurabilityFactor = 0.9F;
        public const float FactoryFacilityPower = -500;
        public const float ControlCenterFacilityPower = -250;
        public const float Epsilon = 0.000000001F;

        public static int PpSize = 32;
        public static float[,] PotentialFields = new float[PpSize, PpSize];

        public static float[,] RangePowerMask5 = CreatePfEx(5);
        public static float[,] RangePowerMask7 = CreatePfEx(7);
        public static float[,] RangePowerMask49 = CreatePfEx(49); //влияние на 2/3 карты

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

        private static double PfFormula1(double x)
        {
            return 1 / (x + 1);
        }

        public static float[,] CreatePfEx(int range)
        {
            if (range % 2 == 0)
            {
                throw new Exception("Требуется нечетное число");
            }

            var result = new float[range, range];

            var centerIndex = range / 2;

            for (int i = 0; i < range; i++)
            {
                for (int j = 0; j < range; j++)
                {
                    var root = GetDistanceTo(i, j, centerIndex, centerIndex);
                    result[i, j] = (float)PfFormula1(root);
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

        public static double GetDistancePower2To(double x1, double y1, double x2, double y2)
        {
            double xRange = x2 - x1;
            double yRange = y2 - y1;
            return xRange * xRange + yRange * yRange;
        }

        public static void AppendEnemyPower(List<List<DbScanHelper.Point>> clusters, bool applyNuclearStrikePower)
        {
            var basePower = EnemyPowerToDodge;

            var currentSelectedGroup = GroupHelper.CurrentGroup;
            var selectedUnits = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains(GroupHelper.CurrentGroup.Id)).ToArray();
            var myGroupPower = selectedUnits
                .Sum(x => BattleHelper.GetPowerHealthMulitplier(currentSelectedGroup.VehicleType, x.Durability))
                * basePower;

            var totalEnemies = UnitHelper.UnitsEnemy.Length;
            var minEnemiesToAttack = totalEnemies * ConfigurationHelper.NuclearStrikeTargetEnemiesCoef;

            foreach (var enemies in clusters)
            {
                var ex = enemies.Sum(x => x.X) / enemies.Count;
                var ey = enemies.Sum(x => x.Y) / enemies.Count;
                var eCellX = (int)ex / PpSize;
                var eCellY = (int)ey / PpSize;

                var res = BattleHelper.CalculatePower(enemies, currentSelectedGroup.VehicleType, basePower);

                var enemyPower = res.EnemyPower;
                var canAttackSomeone = res.CanAttackSomeone;

                //Если готовы нанести ядерный удар
                if (applyNuclearStrikePower)
                {
                    //И врагов в кластере достаточно для удара, сделаем врагов притягиваемыми
                    if (enemies.Count >= minEnemiesToAttack)
                    {
                        ApplyPower(PotentialFields, eCellX, eCellY, RangePowerMask49, -enemyPower);
                    }
                }
                else
                {
                    if (canAttackSomeone)
                    {
                        var pwr = enemyPower - myGroupPower;
                        if (pwr >= 0)
                        {
                            ApplyPower(PotentialFields, eCellX, eCellY, RangePowerMask7, pwr);
                        }
                        else
                        {
                            ApplyPower(PotentialFields, eCellX, eCellY, RangePowerMask49, pwr);
                        }
                    }
                    else
                    {
                        ApplyPower(PotentialFields, eCellX, eCellY, RangePowerMask7, enemyPower);
                    }
                }
            }

        }

        public static void ApplyHealPower()
        {
            var currentSelectedGroup = GroupHelper.CurrentGroup;
            var healPower = HealPower;

            //Сделаем притиягивание к вертолетам или самолетам
            if (currentSelectedGroup.VehicleType == VehicleType.Arrv)
            {
                //var helicopters = UnitHelper.UnitsAlly.Where(x => x.Type == VehicleType.Helicopter).ToList();
                //if (helicopters.Count > 0)
                //{
                //    var cx = helicopters.Sum(x => x.X) / helicopters.Count;
                //    var cy = helicopters.Sum(x => x.Y) / helicopters.Count;

                //    var cellX = (int)cx / PpSize;
                //    var cellY = (int)cy / PpSize;

                //    ApplyPower(PotentialFields, cellX, cellY, RangePowerMask49, -healPower);
                //}

                //var fighters = UnitHelper.UnitsAlly.Where(x => x.Type == VehicleType.Fighter).ToList();
                //if (fighters.Count > 0)
                //{
                //    var cx = fighters.Sum(x => x.X) / fighters.Count;
                //    var cy = fighters.Sum(x => x.Y) / fighters.Count;

                //    var cellX = (int)cx / PpSize;
                //    var cellY = (int)cy / PpSize;

                //    ApplyPower(PotentialFields, cellX, cellY, RangePowerMask49, -healPower);
                //}
            }
            //Сделаем притиягивание самолетов к хилкам, если они ранены
            else if (currentSelectedGroup.VehicleType == VehicleType.Fighter)
            {
                var fighters = UnitHelper.UnitsAlly.Where(x => x.Type == VehicleType.Fighter).ToList();
                if (fighters.Count > 0)
                {
                    var currentDurability = fighters.Sum(x => x.Durability);
                    var maxDurability = GlobalHelper.Game.FighterDurability * fighters.Count;
                    var needHeal = ((float)currentDurability / maxDurability) < FliersToHealDurabilityFactor;

                    if (needHeal)
                    {
                        var healers = UnitHelper.UnitsAlly.Where(x => x.Type == VehicleType.Arrv).ToList();
                        if (healers.Count > 0)
                        {
                            var cx = healers.Sum(x => x.X) / healers.Count;
                            var cy = healers.Sum(x => x.Y) / healers.Count;

                            var cellX = (int)cx / PpSize;
                            var cellY = (int)cy / PpSize;
                            ApplyPower(PotentialFields, cellX, cellY, RangePowerMask49, -healPower);
                        }
                    }
                }
            }
            //Сделаем притиягивание вертолетов к хилкам, если они ранены
            else if (currentSelectedGroup.VehicleType == VehicleType.Helicopter)
            {
                var helicopters = UnitHelper.UnitsAlly.Where(x => x.Type == VehicleType.Helicopter).ToList();
                if (helicopters.Count > 0)
                {
                    var currentDurability = helicopters.Sum(x => x.Durability);
                    var maxDurability = GlobalHelper.Game.HelicopterDurability * helicopters.Count;
                    var needHeal = ((float)currentDurability / maxDurability) < FliersToHealDurabilityFactor;

                    if (needHeal)
                    {
                        var healers = UnitHelper.UnitsAlly.Where(x => x.Type == VehicleType.Arrv).ToList();
                        if (healers.Count > 0)
                        {
                            var cx = healers.Sum(x => x.X) / healers.Count;
                            var cy = healers.Sum(x => x.Y) / healers.Count;

                            var cellX = (int)cx / PpSize;
                            var cellY = (int)cy / PpSize;
                            ApplyPower(PotentialFields, cellX, cellY, RangePowerMask49, -healPower);
                        }
                    }
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
            return GetColorFromRedYellowGreenGradient(fieldValue * 100);
        }

        private static Color GetColorFromRedYellowGreenGradient(double percentage)
        {
            var green = (percentage > 50 ? 1 - 2 * (percentage - 50) / 100.0 : 1.0) * 255;
            var red = (percentage > 50 ? 1.0 : 2 * percentage / 100.0) * 255;
            var blue = 0.0;
            Color result = Color.FromArgb(100, (int)red, (int)green, (int)blue);
            return result;
        }

        /// <summary>
        /// Может вернуть исходную клетку
        /// </summary>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        public static Point2D Get_PP_PointToMove(double cx, double cy, int steps = 1)
        {
            var cellX = (int)cx / PpSize;
            var cellY = (int)cy / PpSize;

            var currentStep = 1;
            Point2D nextPoint;

            do
            {
                var aroundPoints = GetPoinsAround(cellX, cellY);
                var minAroudPointsValue = aroundPoints.Select(x => PotentialFields[(int)x.X, (int)x.Y]).Min();
                nextPoint = aroundPoints.First(x => Math.Abs(PotentialFields[(int)x.X, (int)x.Y] - minAroudPointsValue) < Epsilon);

                cellX = (int)nextPoint.X;
                cellY = (int)nextPoint.Y;

#if DEBUG
                var size = PotentialFieldsHelper.PpSize;
                var quartCellLength = PotentialFieldsHelper.PpSize * 0.25;
                RewindClient.RewindClient.Instance.Rectangle(
                    nextPoint.X * size + quartCellLength,
                    nextPoint.Y * size + quartCellLength,
                    (nextPoint.X + 1) * size - quartCellLength,
                    (nextPoint.Y + 1) * size - quartCellLength,
                    Color.Black);
#endif

            } while (currentStep++ < steps);


            return nextPoint;
        }

        private static List<Point2D> GetPoinsAround(double x, double y)
        {
            var result = new List<Point2D>(9);

            result.Add(new Point2D(x, y));

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
            var currentSelectedGroup = GroupHelper.CurrentGroup;

            MyLivingUnit[] allyUnitsToDodge = new MyLivingUnit[0];

            if (currentSelectedGroup.VehicleType == VehicleType.Fighter ||
                currentSelectedGroup.VehicleType == VehicleType.Helicopter)
            {
                var selectedUnitIds = selectedUnits.Select(x => x.Id).ToArray();
                allyUnitsToDodge = UnitHelper.Units.Values
                    .Where(
                        x => x.Side == Side.Our
                             && (x.Type == VehicleType.Fighter || x.Type == VehicleType.Helicopter)
                             && !selectedUnitIds.Contains(x.Id)).ToArray();
            }
            else if (currentSelectedGroup.VehicleType == VehicleType.Tank ||
                     currentSelectedGroup.VehicleType == VehicleType.Ifv ||
                     currentSelectedGroup.VehicleType == VehicleType.Arrv)
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

        public static void ApplyFacilitiesPower()
        {
            var currentSelectedGroup = GroupHelper.CurrentGroup;
            if (currentSelectedGroup.VehicleType == VehicleType.Fighter ||
                currentSelectedGroup.VehicleType == VehicleType.Helicopter)
            {
                return;
            }

            var notMyFacilities = FacilityHelper.Facilities.Values.Where(x => x.Side != Side.Our).ToArray();

            foreach (var notMyFacility in notMyFacilities)
            {
                var topPpX = (int)(notMyFacility.Left / PpSize);
                var topPpY = (int)(notMyFacility.Top / PpSize);

                for (int i = topPpX; i <= topPpX + 1; i++)
                {
                    for (int j = topPpY; j <= topPpY + 1; j++)
                    {
                        var power = notMyFacility.Type == FacilityType.VehicleFactory
                            ? FactoryFacilityPower
                            : ControlCenterFacilityPower;
                        ApplyPower(PotentialFields, i, j, RangePowerMask49, power);
                    }
                }
            }

            var myFacilitiesControlCenter = FacilityHelper.Facilities.Values
                .Where(x => x.Side != Side.Our)
                .Where(x => x.Type == FacilityType.ControlCenter)
                .ToArray();

            foreach (var myFacilityControlCenter in myFacilitiesControlCenter)
            {
                var topPpX = (int)(myFacilityControlCenter.Left / PpSize);
                var topPpY = (int)(myFacilityControlCenter.Top / PpSize);

                for (int i = topPpX; i <= topPpX + 1; i++)
                {
                    for (int j = topPpY; j <= topPpY + 1; j++)
                    {
                        ApplyPower(PotentialFields, i, j, RangePowerMask49, BaseWorldCenterPower);
                    }
                }
            }
        }
    }
}
