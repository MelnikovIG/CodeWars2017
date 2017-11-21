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

            var enemy = world.Players.First(x => !x.IsMe);

            UpdateVehiclesStates(me, world, game, rewindClient);

#if DEBUG
            DrawNuclearStrikes(me, enemy, game, rewindClient);
#endif

            if (world.TickIndex < 12)
            {
                PrepareUnits();
                return;
            }

            var selectedUnits = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains(CommandsHelper.CurrentSelectedGroup)).ToArray();

            if (selectedUnits.Length == 0)
            {
                if (world.TickIndex % 6 == 0)
                {
                    SelectNextGroup();
                    return;
                }

                return;
            }

            //DrawUnitsVisionRange(selectedUnits, rewindClient);

            if (world.TickIndex % 6 == 0)
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

            if (world.TickIndex % 6 == 0)
            {
                if (me.RemainingNuclearStrikeCooldownTicks <= 0)
                {
                    var nr = game.FighterVisionRange;

                    var minX = selectedUnits.Min(x => x.X);
                    var minY = selectedUnits.Min(x => x.Y);
                    var maxX = selectedUnits.Max(x => x.X);
                    var maxY = selectedUnits.Max(x => x.Y);

                    var minXRange = minX - nr;
                    var minYRange = minY - nr;
                    var maxXRange = maxX + nr;
                    var maxYRange = maxY + nr;

                    rewindClient.Rectangle(minXRange, minYRange, maxXRange, maxYRange, Color.Brown);
                    rewindClient.Rectangle(minX, minY, maxX, maxY, Color.BlueViolet);

                    var potentialEnemiesInRange = UnitHelper.Units.Select(x => x.Value)
                        .Where(x => x.Side == Side.Enemy)
                        .Where(x => x.X > minXRange && x.X < maxXRange)
                        .Where(x => x.Y > minYRange && x.Y < maxYRange)
                        .ToArray();

                    if (CommandsHelper.CurrentSelectedGroup == (int) Groups.H1 ||
                        CommandsHelper.CurrentSelectedGroup == (int) Groups.F1)
                    {
                        potentialEnemiesInRange = potentialEnemiesInRange.Where(x => x.Type != VehicleType.Arrv)
                            .ToArray();
                    }

                    if (potentialEnemiesInRange.Length > 0)
                    {
                        var hasTargetToNuclearAttack = HasTargetToNuclearAttack(selectedUnits, potentialEnemiesInRange);

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

            if (world.TickIndex % 6 == 0)
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
            //Groups.Healer1,
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
        
        private HasTargetToNuclearAttackResult HasTargetToNuclearAttack(MyLivingUnit[] selectedUnits,MyLivingUnit[] potentialEnemiesInRange)
        {
            foreach (var potentialEnemyInRange in potentialEnemiesInRange)
            {
                foreach (var selectedUnit in selectedUnits)
                {
                    var distance = selectedUnit.GetDistanceTo(potentialEnemyInRange);
                    if (distance <= GetVisionRangeByWeather(selectedUnit))
                    {
                        return new HasTargetToNuclearAttackResult()
                        {
                            Success = true,
                            EnemyRes = potentialEnemyInRange,
                            SelectedUnitRes = selectedUnit
                        };
                    }
                }
            }

            return new HasTargetToNuclearAttackResult()
            {
                Success = false,
                EnemyRes = null,
                SelectedUnitRes = null
            };
        }

        private void DrawUnitsVisionRange(MyLivingUnit[] units, RewindClient.RewindClient rewindClient)
        {
            foreach (var unit in units)
            {
                rewindClient.Circle(unit.X, unit.Y, unit.VisionRange, Color.FromArgb(100, 0, 255, 255));
            }
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
                    VisionRange = newVehicle.VisionRange,
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
            var world = GlobalHelper.World;

            if (world.TickIndex == 0)
            {
                SelectUnitsOfType(VehicleType.Fighter);
                return;
            }

            if (world.TickIndex == 1)
            {
                ActionHelper.SetSelectedGroup((int)Groups.F1);
                return;
            }

            if (world.TickIndex == 2)
            {
                var selectedUnitsForScale = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains((int)Groups.F1)).ToArray();
                var xScale = selectedUnitsForScale.Sum(x => x.X) / selectedUnitsForScale.Length;
                var yScale = selectedUnitsForScale.Sum(x => x.Y) / selectedUnitsForScale.Length;

                ActionHelper.Scale(xScale, yScale, 0.1);
                return;
            }

            if (world.TickIndex == 3)
            {
                SelectUnitsOfType(VehicleType.Helicopter);
                return;
            }

            if (world.TickIndex == 4)
            {
                ActionHelper.SetSelectedGroup((int)Groups.H1);
                return;
            }

            if (world.TickIndex == 5)
            {
                var selectedUnitsForScale = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains((int)Groups.H1)).ToArray();
                var xScale = selectedUnitsForScale.Sum(x => x.X) / selectedUnitsForScale.Length;
                var yScale = selectedUnitsForScale.Sum(x => x.Y) / selectedUnitsForScale.Length;

                ActionHelper.Scale(xScale, yScale, 0.1);
                return;
            }

            if (world.TickIndex == 6)
            {
                SelectUnitsOfType(VehicleType.Tank);
                return;
            }

            if (world.TickIndex == 7)
            {
                ActionHelper.SetSelectedGroup((int)Groups.Tank1);
                return;
            }

            if (world.TickIndex == 8)
            {
                var selectedUnitsForScale = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains((int)Groups.Tank1)).ToArray();
                var xScale = selectedUnitsForScale.Sum(x => x.X) / selectedUnitsForScale.Length;
                var yScale = selectedUnitsForScale.Sum(x => x.Y) / selectedUnitsForScale.Length;

                ActionHelper.Scale(xScale, yScale, 0.1);
                return;
            }

            if (world.TickIndex == 9)
            {
                SelectUnitsOfType(VehicleType.Ifv);
                return;
            }

            if (world.TickIndex == 10)
            {
                ActionHelper.SetSelectedGroup((int)Groups.Bmp1);
                return;
            }

            if (world.TickIndex == 11)
            {
                var selectedUnitsForScale = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains((int)Groups.Bmp1)).ToArray();
                var xScale = selectedUnitsForScale.Sum(x => x.X) / selectedUnitsForScale.Length;
                var yScale = selectedUnitsForScale.Sum(x => x.Y) / selectedUnitsForScale.Length;

                ActionHelper.Scale(xScale, yScale, 0.1);
                return;
            }

            //if (world.TickIndex == 12)
            //{
            //    SelectUnitsOfType(VehicleType.Arrv);
            //    return;
            //}

            //if (world.TickIndex == 13)
            //{
            //    ActionHelper.SetSelectedGroup((int)Groups.Healer1);
            //    return;
            //}

            //if (world.TickIndex == 14)
            //{
            //    var selectedUnitsForScale = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains((int)Groups.Healer1)).ToArray();
            //    var xScale = selectedUnitsForScale.Sum(x => x.X) / selectedUnitsForScale.Length;
            //    var yScale = selectedUnitsForScale.Sum(x => x.Y) / selectedUnitsForScale.Length;

            //    ActionHelper.Scale(xScale, yScale, 0.1);
            //    return;
            //}
        }

        private static void SelectUnitsOfType(VehicleType vehicleType)
        {
            var fighters = UnitHelper.Units.Select(x => x.Value).Where(x => x.Side == Side.Our)
                .Where(x => x.Type == vehicleType).ToArray();

            var minX = fighters.Min(x => x.X);
            var minY = fighters.Min(x => x.Y);
            var maxX = fighters.Max(x => x.X);
            var maxY = fighters.Max(x => x.Y);

            var centerX = (minX + maxX) / 2L;
            var centerY = (minY + maxY) / 2L;

            ActionHelper.Select(minX, minY, maxX, maxY /*, vehicleType/);
            //ActionHelper.Select(centerX, centerY, maxX, maxY /*, vehicleType*/);
        }
    }
}