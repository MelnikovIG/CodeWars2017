using System;
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
            var a = game.CloudWeatherVisionFactor;

            GlobalHelper.World = world;
            GlobalHelper.Move = move;
            GlobalHelper.Game = game;

            var rewindClient = RewindClient.RewindClient.Instance;

            var enemy = world.Players.First(x => !x.IsMe);

            UpdateVehiclesStates(me, world, game, rewindClient);

            DrawNuclearStrikes(me, enemy, game, rewindClient);

            if (world.TickIndex == 0)
            {
                var fighters = UnitHelper.Units.Select(x => x.Value).Where(x => x.Side == Side.Our)
                    .Where(x => x.Type == VehicleType.Fighter).ToArray();

                var minX = fighters.Min(x => x.X);
                var minY = fighters.Min(x => x.Y);
                var maxX = fighters.Max(x => x.X);
                var maxY = fighters.Max(x => x.Y);

                var centerX = (minX + maxX) / 2L;
                var centerY = (minY + maxY) / 2L;

                rewindClient.Rectangle(centerX, centerY, maxX, maxY, Color.FromArgb(100, 1, 0, 0));

                ActionHelper.Select(centerX, centerY, maxX, maxY /*, VehicleType.Fighter*/);
                return;
            }

            if (world.TickIndex == 1)
            {
                ActionHelper.SetSelectedGroup(1);
                return;
            }

            var selectedUnits = UnitHelper.Units.Select(x => x.Value).Where(x => x.Groups.Contains(1)).ToArray();

            //DrawUnitsVisionRange(selectedUnits, rewindClient);

            var cx = selectedUnits.Sum(x => x.X) / selectedUnits.Length;
            var cy = selectedUnits.Sum(x => x.Y) / selectedUnits.Length;

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

                    if (potentialEnemiesInRange.Length > 0)
                    {
                        var hasTargetToNuclearAttack = HasTargetToNuclearAttack(selectedUnits, potentialEnemiesInRange);

                        if (hasTargetToNuclearAttack.Success)
                        {
                            var selectedUnit = hasTargetToNuclearAttack.SelectedUnitRes;
                            var enemyUnit = hasTargetToNuclearAttack.EnemyRes;

                            ActionHelper.NuclearStrike(selectedUnit.Id, enemyUnit.X, enemyUnit.Y);
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
                PotentialFieldsHelper.AppendEnemyPower();
            }
            PotentialFieldsHelper.AppendAllyFlyingPowerForFlyingUnits(selectedUnits);
            PotentialFieldsHelper.Normalize();
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var color = PotentialFieldsHelper.GetColorFromValue(PotentialFieldsHelper.PotentialFields[i, j]);

                    rewindClient.Rectangle(i * size, j * size, (i + 1) * size, (j + 1) * size, color);
                }
            }

            rewindClient.End();

            var nextPpPoint = PotentialFieldsHelper.GetNextSafest_PP_PointByWorldXY(cx, cy);
            rewindClient.Rectangle(nextPpPoint.X * size, nextPpPoint.Y * size, (nextPpPoint.X + 1) * size,
                (nextPpPoint.Y + 1) * size, Color.Black);

            if (world.TickIndex % 6 == 0)
            {
                var vx = nextPpPoint.X * size + size / 2d - cx;
                var vy = nextPpPoint.Y * size + size / 2d - cy;
                ActionHelper.Move(vx, vy);

                //Console.WriteLine($"vx {vx}\tvy {vy}\tRemainingActionCooldownTicks {me.RemainingActionCooldownTicks}");
            }
            return;
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
            }
#endif
        }

        private double GetVisionRangeByWeather(MyLivingUnit livingUnit)
        {
            if (livingUnit.Type == VehicleType.Fighter)
            {
                var x = (int)livingUnit.X / PotentialFieldsHelper.PpSize;
                var y = (int)livingUnit.Y / PotentialFieldsHelper.PpSize;

                double scale = 1;
                var weaterType = GlobalHelper.World.WeatherByCellXY[x][y];
                if (weaterType == WeatherType.Clear)
                {
                    scale = GlobalHelper.Game.ClearWeatherVisionFactor;
                }
                else if (weaterType == WeatherType.Cloud)
                {
                    scale = GlobalHelper.Game.CloudWeatherVisionFactor;
                }
                else if (weaterType == WeatherType.Rain)
                {
                    scale = GlobalHelper.Game.RainWeatherVisionFactor;
                }

                var visionRange = GlobalHelper.Game.FighterVisionRange * scale;
                return visionRange;
            }

            throw new NotImplementedException();
        }
    }
}