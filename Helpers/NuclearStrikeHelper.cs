using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class NuclearStrikeHelper
    {
        private static IGrouping<int, MyLivingUnit>[] groupsInNuclearStrike;
        public static NuclearStrikeState NuclearStrikeState { get; set; } = NuclearStrikeState.None;
        public static Player Enemy => GlobalHelper.Enemy;
        public static bool IsEnemyNuclearStrikeExecuting => Enemy.NextNuclearStrikeTickIndex >= 0;

        private static double LastEnemyNuclearStrikeX { get; set; }
        private static double LastEnemyNuclearStrikeY { get; set; }

        public static bool ProcessEnemyNuclearStrikeDodge(bool moveAllowed)
        {
            var isEmenyExecutingNs = IsEnemyNuclearStrikeExecuting;

            if (isEmenyExecutingNs)
            {

                LastEnemyNuclearStrikeX = Enemy.NextNuclearStrikeX;
                LastEnemyNuclearStrikeY = Enemy.NextNuclearStrikeY;

                switch (NuclearStrikeState)
                {
                    case NuclearStrikeState.None:
                        var allyUnitsInRangeOfNuclearStrike = GetAllyUnitsInRangeOfNuclearStrike();
                        if (allyUnitsInRangeOfNuclearStrike.Length > 0)
                        {
                             groupsInNuclearStrike = allyUnitsInRangeOfNuclearStrike
                                .Where(x => x.Groups.Length > 0)
                                .GroupBy(x => x.Groups[0])
                                .ToArray();

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
            var groupLength = groupsInNuclearStrike.Length;

            //TODO: useMoveAllowed 
            foreach (var group in groupsInNuclearStrike)
            {
                var chosenGroup = GroupHelper.Groups[group.Key - 1];

                if (GroupHelper.CurrentGroup != chosenGroup)
                {
                    //Был баг с 2мя действиями за ход
                    if (moveAllowed && groupLength < 2)
                    {
                        ActionHelper.SelectGroup(chosenGroup);
                    }
                    else
                    {
                        QueueHelper.Queue.Enqueue(new SelectGroup(chosenGroup));
                    }
                    QueueHelper.Queue.Enqueue(new Scale(LastEnemyNuclearStrikeX, LastEnemyNuclearStrikeY, 10));
                }
                else
                {
                    //Был баг с 2мя действиями за ход
                    if (moveAllowed && groupLength < 2)
                    {
                        ActionHelper.Scale(LastEnemyNuclearStrikeX, LastEnemyNuclearStrikeY, 10);
                    }
                    else
                    {
                        QueueHelper.Queue.Enqueue(new Scale(LastEnemyNuclearStrikeX, LastEnemyNuclearStrikeY, 10));
                    }
                }

            }

            //var nsRadius = GlobalHelper.Game.TacticalNuclearStrikeRadius;

            //if (moveAllowed)
            //{
            //    ActionHelper.Select(
            //        LastEnemyNuclearStrikeX - nsRadius,
            //        LastEnemyNuclearStrikeY - nsRadius,
            //        LastEnemyNuclearStrikeX + nsRadius,
            //        LastEnemyNuclearStrikeY + nsRadius
            //    );
            //    QueueHelper.Queue.Enqueue(new Scale(LastEnemyNuclearStrikeX, LastEnemyNuclearStrikeY, 10));
            //}
            //else
            //{
            //    QueueHelper.Queue.Enqueue(new SelectUnits(
            //        LastEnemyNuclearStrikeX - nsRadius,
            //        LastEnemyNuclearStrikeY - nsRadius,
            //        LastEnemyNuclearStrikeX + nsRadius,
            //        LastEnemyNuclearStrikeY + nsRadius
            //        ));
            //    QueueHelper.Queue.Enqueue(new Scale(LastEnemyNuclearStrikeX, LastEnemyNuclearStrikeY, 10));
            //}
        }

        private static void MakeGather(bool moveAllowed)
        {
            //TODO: useMoveAllowed (aware multimove if many groups)
            

            foreach (var group in groupsInNuclearStrike)
            {
                var chosenGroup = GroupHelper.Groups[group.Key - 1];

                if (GroupHelper.CurrentGroup != chosenGroup)
                {
                    QueueHelper.Queue.Enqueue(new SelectGroup(chosenGroup));
                }

                for (int i = 0; i < 7; i++)
                {
                    QueueHelper.Queue.Enqueue(new Scale(LastEnemyNuclearStrikeX, LastEnemyNuclearStrikeY, 0.1));
                }
            }
        }


        public class HasTargetToNuclearAttackResult
        {
            public bool Success { get; set; }
            public MyLivingUnit SelectedUnitRes { get; set; }
            public MyLivingUnit EnemyRes { get; set; }
        }

        public static HasTargetToNuclearAttackResult HasTargetToNuclearAttack(MyLivingUnit[] selectedUnits)
        {
            var allEnemiesCanBeAttacked = new Dictionary<long, List<MyLivingUnit>>();

            foreach (var selectedUnit in selectedUnits)
            {
                var enemyUnitsInRange = UnitHelper.UnitsEnemy
                    .Where(x =>
                    {
                        var visionRange = GetVisionRangeByWeather(selectedUnit);
                        return GeometryHelper.PointIsWithinCircle(selectedUnit.X, selectedUnit.Y, visionRange, x.X, x.Y);
                    }).ToArray();

                foreach (var enemyUnitInRange in enemyUnitsInRange)
                {
                    var enemyId = enemyUnitInRange.Id;

                    if (!allEnemiesCanBeAttacked.ContainsKey(enemyId))
                    {
                        allEnemiesCanBeAttacked[enemyId] = new List<MyLivingUnit>();
                    }

                    allEnemiesCanBeAttacked[enemyId].Add(selectedUnit);
                }
            }

            if (allEnemiesCanBeAttacked.Count == 0)
            {
                return new HasTargetToNuclearAttackResult()
                {
                    Success = false
                };
            }

            var enemiesCanBeAttacked = allEnemiesCanBeAttacked.Select(x => UnitHelper.Units[x.Key]).ToList();
            var nsRange = GlobalHelper.Game.TacticalNuclearStrikeRadius;
            var maxNsDamage = GlobalHelper.Game.MaxTacticalNuclearStrikeDamage;

            var enemiesWithDamage = new List<Tuple<MyLivingUnit, double>>(allEnemiesCanBeAttacked.Count);

            var totalEnemies = UnitHelper.UnitsEnemy.Length;
            var minEnemiesToAttack = totalEnemies * ConfigurationHelper.NuclearStrikeTargetEnemiesCoef;

            foreach (var enemyCanBeAttacked in enemiesCanBeAttacked)
            {
                var allEnemiesFromEnemyRange = UnitHelper.UnitsEnemy
                    .Where(x => GeometryHelper.PointIsWithinCircle(enemyCanBeAttacked.X, enemyCanBeAttacked.Y,
                        nsRange, x.X, x.Y)).ToArray();

                if (allEnemiesFromEnemyRange.Length < minEnemiesToAttack)
                {
                    continue;
                }

                double totalDamage = 0;

                foreach (var enemyFromEnemyRange in allEnemiesFromEnemyRange)
                {
                    var distance = PotentialFieldsHelper.GetDistanceTo(enemyCanBeAttacked.X, enemyCanBeAttacked.Y,
                        enemyFromEnemyRange.X, enemyFromEnemyRange.Y);

                    //Урон - это расстояние от эпиценра * урон
                    var damage = ((nsRange - distance) / nsRange) * maxNsDamage;

                     var speedDamageCoef = GetDamageCoefficientByVehicleTypeSpeed(enemyFromEnemyRange.Type);

                    damage *= speedDamageCoef;

                    if (damage > enemyFromEnemyRange.Durability)
                    {
                        totalDamage += maxNsDamage;
                    }
                    else
                    {
                        totalDamage += damage;
                    }
                }

                var allAlliesFromEnemyRange = UnitHelper.UnitsAlly
                    .Where(x => GeometryHelper.PointIsWithinCircle(enemyCanBeAttacked.X, enemyCanBeAttacked.Y,
                        nsRange, x.X, x.Y)).ToArray();

                foreach (var allyFromEnemyRange in allAlliesFromEnemyRange)
                {
                    var distance = PotentialFieldsHelper.GetDistanceTo(enemyCanBeAttacked.X, enemyCanBeAttacked.Y,
                        allyFromEnemyRange.X, allyFromEnemyRange.Y);

                    //Урон - это расстояние от эпиценра * урон
                    var damage = ((nsRange - distance) / nsRange) * maxNsDamage;

                    if (damage > allyFromEnemyRange.Durability)
                    {
                        totalDamage -= 100;
                    }
                    else
                    {
                        totalDamage -= damage;
                    }
                }

                enemiesWithDamage.Add(new Tuple<MyLivingUnit, double>(enemyCanBeAttacked, totalDamage));
            }

            if (enemiesWithDamage.Count == 0)
            {
                return new HasTargetToNuclearAttackResult()
                {
                    Success = false
                };
            }

            var maxTotalDamage = enemiesWithDamage.Select(x => x.Item2).Max();

            //Выгода по урону не в пользу нас, не планируем удар
            if (maxTotalDamage < 0)
            {
                return new HasTargetToNuclearAttackResult()
                {
                    Success = false
                };
            }

            var enemyUnitWithMaxDamage = enemiesWithDamage.First(x => Math.Abs(x.Item2 - maxTotalDamage) < 0.00000001).Item1;

#if DEBUG
            RewindClient.RewindClient.Instance.Circle(enemyUnitWithMaxDamage.X, enemyUnitWithMaxDamage.Y, enemyUnitWithMaxDamage.Radius * 3, Color.Red);
#endif

            var alliesCanAttackEnemyWithMaxDamage = allEnemiesCanBeAttacked[enemyUnitWithMaxDamage.Id];
            var alliesWithRange = alliesCanAttackEnemyWithMaxDamage.Select(x => new
            {
                Ally = x,
                Range = PotentialFieldsHelper.GetDistancePower2To(x.X, x.Y, enemyUnitWithMaxDamage.X,
                    enemyUnitWithMaxDamage.Y)
            }).ToList();

            var maxRange = alliesWithRange.Select(x => x.Range).Max();
            var ally = alliesWithRange.First(x => Math.Abs(x.Range - maxRange) < 0.00000001).Ally;

#if DEBUG
            RewindClient.RewindClient.Instance.Circle(ally.X, ally.Y, ally.Radius * 3, Color.Purple);

            var vr = GetVisionRangeByWeather(ally);
            RewindClient.RewindClient.Instance.Circle(ally.X, ally.Y, vr, Color.FromArgb(100, 255, 0, 200));
#endif

            return new HasTargetToNuclearAttackResult()
            {
                Success = true,
                SelectedUnitRes = ally,
                EnemyRes = enemyUnitWithMaxDamage
            };
        }

        //TODO: cacheOnceToDictionary
        private static double GetDamageCoefficientByVehicleTypeSpeed(VehicleType vehicleTypeype)
        {
            double coef = 1;

            var minSpeed = new[]
            {
                GlobalHelper.Game.FighterSpeed,
                GlobalHelper.Game.HelicopterSpeed,
                GlobalHelper.Game.TankSpeed,
                GlobalHelper.Game.IfvSpeed,
                GlobalHelper.Game.ArrvSpeed,
            }.Min();

            if (vehicleTypeype == VehicleType.Fighter)
            {
                coef *= minSpeed / GlobalHelper.Game.FighterSpeed;
            }
            else if (vehicleTypeype == VehicleType.Helicopter)
            {
                coef *= minSpeed / GlobalHelper.Game.HelicopterSpeed;
            }
            else if (vehicleTypeype == VehicleType.Tank)
            {
                coef *= minSpeed / GlobalHelper.Game.TankSpeed;
            }
            else if (vehicleTypeype == VehicleType.Ifv)
            {
                coef *= minSpeed / GlobalHelper.Game.IfvSpeed;
            }
            else if (vehicleTypeype == VehicleType.Arrv)
            {
                coef *= minSpeed / GlobalHelper.Game.ArrvSpeed;
            }
            else
            {
                throw GlobalHelper.GetException("GetDamageCoefficientByVehicleTypeSpeed unknown type");
            }

            return coef;
        }

        private static double GetVisionRangeByWeather(MyLivingUnit livingUnit)
        {
            var x = (int)livingUnit.X / PotentialFieldsHelper.PpSize;
            var y = (int)livingUnit.Y / PotentialFieldsHelper.PpSize;

            double airScale = 1;
            double groundScale = 1;

            var weaterType = GlobalHelper.World.WeatherByCellXY[x][y];
            if (weaterType == WeatherType.Clear)
            {
                airScale = GlobalHelper.Game.ClearWeatherVisionFactor;
            }
            else if (weaterType == WeatherType.Cloud)
            {
                airScale = GlobalHelper.Game.CloudWeatherVisionFactor;
            }
            else if (weaterType == WeatherType.Rain)
            {
                airScale = GlobalHelper.Game.RainWeatherVisionFactor;
            }

            var terrainType = GlobalHelper.World.TerrainByCellXY[x][y];
            if (terrainType == TerrainType.Plain)
            {
                groundScale = GlobalHelper.Game.PlainTerrainVisionFactor;
            }
            else if (terrainType == TerrainType.Forest)
            {
                groundScale = GlobalHelper.Game.ForestTerrainVisionFactor;
            }
            else if (terrainType == TerrainType.Swamp)
            {
                groundScale = GlobalHelper.Game.SwampTerrainVisionFactor;
            }

            if (livingUnit.Type == VehicleType.Fighter)
            {
                var visionRange = GlobalHelper.Game.FighterVisionRange * airScale;
                return visionRange;
            }
            else if (livingUnit.Type == VehicleType.Helicopter)
            {
                var visionRange = GlobalHelper.Game.HelicopterVisionRange * airScale;
                return visionRange;
            }
            else if (livingUnit.Type == VehicleType.Tank)
            {
                var visionRange = GlobalHelper.Game.TankVisionRange * groundScale;
                return visionRange;
            }
            else if (livingUnit.Type == VehicleType.Ifv)
            {
                var visionRange = GlobalHelper.Game.IfvVisionRange * groundScale;
                return visionRange;
            }
            else if (livingUnit.Type == VehicleType.Arrv)
            {
                var visionRange = GlobalHelper.Game.ArrvVisionRange * groundScale;
                return visionRange;
            }

            throw new NotImplementedException();
        }

#if DEBUG
        public static void DrawNuclearStrikes(Player me, Player enemy, Game game, RewindClient.RewindClient rewindClient)
        {
            if (me.NextNuclearStrikeTickIndex > 0)
            {
                var nx = me.NextNuclearStrikeX;
                var ny = me.NextNuclearStrikeY;
                var nr = game.TacticalNuclearStrikeRadius;

                var nunit = UnitHelper.Units[me.NextNuclearStrikeVehicleId];

                rewindClient.Circle(nx, ny, nr, Color.FromArgb(150, 225, 0, 0));
                rewindClient.Circle(nunit.X, nunit.Y, nunit.Radius * 2, Color.Black);
                rewindClient.Line(nunit.X, nunit.Y, nx, ny, Color.Black);

                var nsRange = PotentialFieldsHelper.GetDistanceTo(nunit.X, nunit.Y, nx, ny);
                rewindClient.Message($"NuclearStrikeDistanse: {nsRange}");
            }
            if (enemy.NextNuclearStrikeTickIndex > 0)
            {
                var nx = enemy.NextNuclearStrikeX;
                var ny = enemy.NextNuclearStrikeY;
                var nr = game.TacticalNuclearStrikeRadius;

                var nunit = UnitHelper.Units[enemy.NextNuclearStrikeVehicleId];
                rewindClient.Circle(nx, ny, nr, Color.FromArgb(150, 225, 0, 0));
                rewindClient.Circle(nunit.X, nunit.Y, nunit.Radius * 2, Color.Black);
            }
        }
#endif
    }

    public enum NuclearStrikeState
    {
        None,
        Spread,
        Gather
    }
}
