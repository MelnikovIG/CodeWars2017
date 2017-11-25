using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;
using RewindClient;
using Side = Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers.Side;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public sealed class MyStrategy : IStrategy
    {
        private static int PrepareStep = 0;
        private static bool Prepared = false;

        public void Move(Player me, World world, Game game, Move move)
        {
            var rewindClient = RewindClient.RewindClient.Instance;

            MoveEx(me, world, game, move, rewindClient);

            rewindClient.End();
        }

        public void MoveEx(Player me, World world, Game game, Move move, RewindClient.RewindClient rewindClient)
        {
            GlobalHelper.World = world;
            GlobalHelper.Move = move;
            GlobalHelper.Game = game;
            GlobalHelper.Me = me;
            GlobalHelper.Enemy = world.GetOpponentPlayer();

            var enemy = GlobalHelper.Enemy;

            UpdateVehiclesStates(me, world, game, rewindClient);

#if DEBUG
            DrawNuclearStrikes(me, enemy, game, rewindClient);
#endif

            if (!Prepared)
            {
                if (!GlobalHelper.MoveAllowed)
                {
                    return;
                }

                PrepareUnits();

                if (!Prepared)
                {
                    PrepareStep++;
                    return;
                }
            }

            var selectedUnits = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains((int)CommandsHelper.CurrentSelectedGroup)).ToArray();

            if (selectedUnits.Length == 0)
            {
                if (GlobalHelper.MoveAllowed)
                {
                    SelectNextGroup();
                    return;
                }

                return;
            }

            if (GlobalHelper.MoveAllowed)
            {
                if (me.NextNuclearStrikeTickIndex > 0)
                {
                    var isNucleatorInGroup = selectedUnits.Select(x => x.Id).Contains(me.NextNuclearStrikeVehicleId);
                    if (isNucleatorInGroup)
                    {
                        var nucleator = selectedUnits.FirstOrDefault(x => x.Id == me.NextNuclearStrikeVehicleId);
                        if (nucleator != null)
                        {
                            ActionHelper.Move(0, 0);
                            return;
                        }
                    }
                }
            }

            if (GlobalHelper.MoveAllowed)
            {
                if (me.RemainingNuclearStrikeCooldownTicks <= 0)
                {
                    var hasTargetToNuclearAttack = HasTargetToNuclearAttack(selectedUnits);

                    if (hasTargetToNuclearAttack.Success)
                    {
                        //Если остановились для выстрела, высрелим
                        if (CommandsHelper.Commands.Last().CommandType == CommandType.StopMove)
                        {
                            var selectedUnit = hasTargetToNuclearAttack.SelectedUnitRes;
                            var enemyUnit = hasTargetToNuclearAttack.EnemyRes;

                            ActionHelper.NuclearStrike(selectedUnit.Id, enemyUnit.X, enemyUnit.Y);
                            return;
                        }
                        //Иначе остановимся для выстрела на след ходу
                        else
                        {
                            ActionHelper.StopMove();
                            return;
                        }
                    }
                }
            }

            var size = PotentialFieldsHelper.PpSize;
            PotentialFieldsHelper.Clear();
            if (me.RemainingNuclearStrikeCooldownTicks <= 0)
            {
                PotentialFieldsHelper.ApplyPowerToNuclearStrike();
            }
            else
            {
                PotentialFieldsHelper.AppendEnemyPowerToDodge();
                PotentialFieldsHelper.ApplyHealPower();
            }
            PotentialFieldsHelper.AppendAllyUnitsToDodge(selectedUnits);
            PotentialFieldsHelper.Normalize();
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var color = PotentialFieldsHelper.GetColorFromValue(PotentialFieldsHelper.PotentialFields[i, j]);

                    rewindClient.Rectangle(i * size, j * size, (i + 1) * size, (j + 1) * size, color);
                }
            }

            if (GlobalHelper.MoveAllowed)
            {
                //Если на предыдущем ходу текущая группа уже двигалась, передадим управление след группе
                if (CommandsHelper.Commands.Last().CommandType == CommandType.Move)
                {
                    var isSelectSuccess = SelectNextGroup();
                    if (isSelectSuccess)
                    {
                        return;
                    }
                }

                var cx = selectedUnits.Sum(x => x.X) / selectedUnits.Length;
                var cy = selectedUnits.Sum(x => x.Y) / selectedUnits.Length;

                var nextPpPoint = PotentialFieldsHelper.GetNextSafest_PP_PointByWorldXY(cx, cy);
                rewindClient.Rectangle(nextPpPoint.X * size, nextPpPoint.Y * size, (nextPpPoint.X + 1) * size,
                    (nextPpPoint.Y + 1) * size, Color.Black);

                var vx = nextPpPoint.X * size + size / 2d - cx;
                var vy = nextPpPoint.Y * size + size / 2d - cy;
                ActionHelper.Move(vx, vy);
            }
            return;
        }

        private List<Groups> GroupsList = new List<Groups>()
        {
            Groups.F1,
            Groups.H1,
            Groups.Tank1,
            Groups.Bmp1,
            Groups.Healer1,
        };

        private bool SelectNextGroup()
        {
            var currentGroup = (Groups) CommandsHelper.CurrentSelectedGroup;
            var currentGroupIndex = GroupsList.IndexOf(currentGroup);

            if (currentGroupIndex < 0)
            {
                throw new NotImplementedException("currentGroupIndex < 0");
            }

            Groups newGroupType;

            do
            {
                var nextGroupIdx = currentGroupIndex == GroupsList.Count - 1 ? 0 : currentGroupIndex + 1;
                newGroupType = GroupsList[nextGroupIdx];

                var newGroupUnitsCount = UnitHelper.UnitsAlly
                    .Where(x => x.Groups.Contains((int) newGroupType)).ToArray();

                if (newGroupUnitsCount.Length > 0)
                {
                    ActionHelper.SelectGroup((int) newGroupType);
                    return true;
                }
                currentGroupIndex = nextGroupIdx;

            } while (newGroupType != currentGroup);

            return false;
        }

        private class HasTargetToNuclearAttackResult
        {
            public bool Success { get; set; }
            public MyLivingUnit SelectedUnitRes { get; set; }
            public MyLivingUnit EnemyRes { get; set; }
        }

        private double GetVisionRangeOfCurrentSelectedUnutType()
        {
            if (CommandsHelper.CurrentSelectedGroup == Groups.F1)
            {
                return GlobalHelper.Game.FighterVisionRange;
            }
            else if (CommandsHelper.CurrentSelectedGroup == Groups.H1)
            {
                return GlobalHelper.Game.HelicopterVisionRange;
            }
            else if (CommandsHelper.CurrentSelectedGroup == Groups.Tank1)
            {
                return GlobalHelper.Game.TankVisionRange;
            }
            else if (CommandsHelper.CurrentSelectedGroup == Groups.Bmp1)
            {
                return GlobalHelper.Game.IfvVisionRange;
            }
            else if (CommandsHelper.CurrentSelectedGroup == Groups.Healer1)
            {
                return GlobalHelper.Game.ArrvVisionRange;
            }

            throw new NotImplementedException();
        }

        private HasTargetToNuclearAttackResult HasTargetToNuclearAttack(MyLivingUnit[] selectedUnits)
        {
            var allEnemiesCanBeAttacked = new Dictionary<long, List<MyLivingUnit>>();

            foreach (var selectedUnit in selectedUnits)
            {
                var enemyUnitsInRange = UnitHelper.UnitsEnemy
                    .Where(x =>
                    {
                        var visionRange = GetVisionRangeByWeather(selectedUnit);
                        return PotentialFieldsHelper.PointIsWithinCircle(selectedUnit.X, selectedUnit.Y, visionRange, x.X, x.Y);
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
            var nsDamage = GlobalHelper.Game.MaxTacticalNuclearStrikeDamage;

            var enemiesWithDamage = new List<Tuple<MyLivingUnit, double>>(allEnemiesCanBeAttacked.Count);

            foreach (var enemyCanBeAttacked in enemiesCanBeAttacked)
            {
                double totalDamage = 0;
                var allEnemiesFromEnemyRange = UnitHelper.UnitsEnemy
                    .Where(x => PotentialFieldsHelper.PointIsWithinCircle(enemyCanBeAttacked.X, enemyCanBeAttacked.Y,
                        nsRange, x.X, x.Y)).ToArray();

                foreach (var enemyFromEnemyRange in allEnemiesFromEnemyRange)
                {
                    var distance = PotentialFieldsHelper.GetDistanceTo(enemyCanBeAttacked.X, enemyCanBeAttacked.Y,
                        enemyFromEnemyRange.X, enemyFromEnemyRange.Y);

                    //Урон - это расстояние от эпиценра * урон
                    var damage = ((nsRange - distance) / nsRange) * nsDamage;

                    if (damage > enemyFromEnemyRange.Durability)
                    {
                        totalDamage += 100;
                    }
                    else
                    {
                        totalDamage += damage;
                    }
                }

                var allAlliesFromEnemyRange = UnitHelper.UnitsAlly
                    .Where(x => PotentialFieldsHelper.PointIsWithinCircle(enemyCanBeAttacked.X, enemyCanBeAttacked.Y,
                        nsRange, x.X, x.Y)).ToArray();

                foreach (var allyFromEnemyRange in allAlliesFromEnemyRange)
                {
                    var distance = PotentialFieldsHelper.GetDistanceTo(enemyCanBeAttacked.X, enemyCanBeAttacked.Y,
                        allyFromEnemyRange.X, allyFromEnemyRange.Y);

                    //Урон - это расстояние от эпиценра * урон
                    var damage = ((nsRange - distance) / nsRange) * nsDamage;

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
            RewindClient.RewindClient.Instance.Circle(ally.X, ally.Y, vr, Color.FromArgb(100, 255,0,200));
#endif

            //TODO: вернуть результат
            return new HasTargetToNuclearAttackResult()
            {
                Success = true,
                SelectedUnitRes = ally,
                EnemyRes = enemyUnitWithMaxDamage
            };
        }

#if DEBUG
        private void DrawNuclearStrikes(Player me, Player enemy, Game game, RewindClient.RewindClient rewindClient)
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

        private RewindClient.UnitType GetRewindClientUitType(VehicleType vehicleType)
        {
            switch (vehicleType)
            {
                case VehicleType.Arrv: return UnitType.Arrv;
                case VehicleType.Fighter: return UnitType.Fighter;
                case VehicleType.Helicopter: return UnitType.Helicopter;
                case VehicleType.Ifv: return UnitType.Ifv;
                case VehicleType.Tank: return UnitType.Tank;
                default: return UnitType.Unknown;
            }
        }

        public void UpdateVehiclesStates(Player me, World world, Game game, RewindClient.RewindClient rewindClient)
        {
            var myId = me.Id;

            foreach (var newVehicle in world.NewVehicles)
            {
                UnitHelper.Units.Add(newVehicle.Id, new MyLivingUnit()
                {
                    Id = newVehicle.Id,
                    X = newVehicle.X,
                    Y = newVehicle.Y,
                    Radius = newVehicle.Radius,
                    Durability = newVehicle.Durability,
                    MaxDurability = newVehicle.MaxDurability,
                    Side = newVehicle.PlayerId == myId ? Side.Our : Side.Enemy,
                    IsSelected = newVehicle.IsSelected,
                    Type = newVehicle.Type
                });
            }

            foreach (var vehicleUpdate in world.VehicleUpdates)
            {
                var vehicle = UnitHelper.Units[vehicleUpdate.Id];
                vehicle.X = vehicleUpdate.X;
                vehicle.Y = vehicleUpdate.Y;
                vehicle.Durability = vehicleUpdate.Durability;
                vehicle.Groups = vehicleUpdate.Groups;
                vehicle.IsSelected = vehicleUpdate.IsSelected;
            }

            var emptyUnits = UnitHelper.Units.Where(x => x.Value.Durability <= 0).ToArray();
            if (emptyUnits.Length > 0)
            {
                foreach (var emptyUnit in emptyUnits){
                    UnitHelper.Units.Remove(emptyUnit.Key);
                }
            }


#if DEBUG
            foreach (var unit in UnitHelper.Units.Values)
            {

                rewindClient.LivingUnit(
                    unit.X,
                    unit.Y,
                    game.VehicleRadius,
                    unit.Durability,
                    unit.MaxDurability,
                    (RewindClient.Side) unit.Side,
                    0,
                    GetRewindClientUitType(unit.Type));

                if (unit.IsSelected)
                {
                    rewindClient.Circle(unit.X,
                        unit.Y,
                        game.VehicleRadius * 3,
                        Color.FromArgb(200, 255, 255, 0));
                }
            }
#endif
        }

        private double GetVisionRangeByWeather(MyLivingUnit livingUnit)
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

        private static void PrepareUnits()
        {
            if (PrepareStep == 0)
            {
                SelectUnitsOfType(VehicleType.Fighter);
                return;
            }

            if (PrepareStep == 1)
            {
                ActionHelper.SetSelectedGroup((int)Groups.F1);
                return;
            }

            if (PrepareStep == 2)
            {
                var selectedUnitsForScale = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains((int)Groups.F1)).ToArray();
                var xScale = selectedUnitsForScale.Sum(x => x.X) / selectedUnitsForScale.Length;
                var yScale = selectedUnitsForScale.Sum(x => x.Y) / selectedUnitsForScale.Length;

                ActionHelper.Scale(xScale, yScale, 0.1);
                return;
            }

            if (PrepareStep == 3)
            {
                SelectUnitsOfType(VehicleType.Helicopter);
                return;
            }

            if (PrepareStep == 4)
            {
                ActionHelper.SetSelectedGroup((int)Groups.H1);
                return;
            }

            if (PrepareStep == 5)
            {
                var selectedUnitsForScale = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains((int)Groups.H1)).ToArray();
                var xScale = selectedUnitsForScale.Sum(x => x.X) / selectedUnitsForScale.Length;
                var yScale = selectedUnitsForScale.Sum(x => x.Y) / selectedUnitsForScale.Length;

                ActionHelper.Scale(xScale, yScale, 0.1);
                return;
            }

            if (PrepareStep == 6)
            {
                SelectUnitsOfType(VehicleType.Tank);
                return;
            }

            if (PrepareStep == 7)
            {
                ActionHelper.SetSelectedGroup((int)Groups.Tank1);
                return;
            }

            if (PrepareStep == 8)
            {
                var selectedUnitsForScale = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains((int)Groups.Tank1)).ToArray();
                var xScale = selectedUnitsForScale.Sum(x => x.X) / selectedUnitsForScale.Length;
                var yScale = selectedUnitsForScale.Sum(x => x.Y) / selectedUnitsForScale.Length;

                ActionHelper.Scale(xScale, yScale, 0.1);
                return;
            }

            if (PrepareStep == 9)
            {
                SelectUnitsOfType(VehicleType.Ifv);
                return;
            }

            if (PrepareStep == 10)
            {
                ActionHelper.SetSelectedGroup((int)Groups.Bmp1);
                return;
            }

            if (PrepareStep == 11)
            {
                var selectedUnitsForScale = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains((int)Groups.Bmp1)).ToArray();
                var xScale = selectedUnitsForScale.Sum(x => x.X) / selectedUnitsForScale.Length;
                var yScale = selectedUnitsForScale.Sum(x => x.Y) / selectedUnitsForScale.Length;

                ActionHelper.Scale(xScale, yScale, 0.1);
                return;
            }

            if (PrepareStep == 12)
            {
                SelectUnitsOfType(VehicleType.Arrv);
                return;
            }

            if (PrepareStep == 13)
            {
                ActionHelper.SetSelectedGroup((int)Groups.Healer1);
                return;
            }

            if (PrepareStep == 14)
            {
                var selectedUnitsForScale = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains((int)Groups.Healer1)).ToArray();
                var xScale = selectedUnitsForScale.Sum(x => x.X) / selectedUnitsForScale.Length;
                var yScale = selectedUnitsForScale.Sum(x => x.Y) / selectedUnitsForScale.Length;

                ActionHelper.Scale(xScale, yScale, 0.1);
                return;
            }

            if (PrepareStep == 15)
            {
                SelectUnitsOfType(VehicleType.Helicopter);
                return;
            }

            if (PrepareStep == 16)
            {
                Prepared = true;
            }
        }

        private static void SelectUnitsOfType(VehicleType vehicleType)
        {
            var fighters = UnitHelper.Units.Select(x => x.Value).Where(x => x.Side == Side.Our)
                .Where(x => x.Type == vehicleType).ToArray();

            var minX = fighters.Min(x => x.X);
            var minY = fighters.Min(x => x.Y);
            var maxX = fighters.Max(x => x.X);
            var maxY = fighters.Max(x => x.Y);

            ActionHelper.Select(minX, minY, maxX, maxY);
        }
    }
}