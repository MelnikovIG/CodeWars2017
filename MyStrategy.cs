using System;
using System.Drawing;
using System.Linq;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;
using RewindClient;
using Side = Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers.Side;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk {
    public sealed class MyStrategy : IStrategy {
        public void Move(Player me, World world, Game game, Move move) {
            GlobalHelper.World = world;
            GlobalHelper.Move = move;

            var myId = me.Id;

            var rewindClient = RewindClient.RewindClient.Instance;

            foreach(var newVehicle in world.NewVehicles){
                UnitHelper.Units.Add(newVehicle.Id, new MyLivingUnit()
                {
                    Id = newVehicle.Id,
                    X = newVehicle.X,
                    Y = newVehicle.Y,
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

            foreach (var unit in UnitHelper.Units.Values)
            {
                //rewindClient.Circle(vehicleUpdate.X, vehicleUpdate.Y, game.VehicleRadius, Color.Red);
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

                rewindClient.Rectangle(centerX,centerY, maxX, maxY,Color.FromArgb(100,1,0,0));

                ActionHelper.Select(centerX, centerY, maxX, maxY/*, VehicleType.Fighter*/);
                return;
            }

            if (world.TickIndex == 1)
            {
                ActionHelper.SetSelectedGroup(1);
                return;
            }

            var selectedUnits = UnitHelper.Units.Select(x => x.Value).Where(x => x.Groups.Contains(1)).ToArray();

            if (selectedUnits.Length == 10)
            {
                
            }

            var cx = selectedUnits.Sum(x => x.X) / selectedUnits.Length;
            var cy = selectedUnits.Sum(x => x.Y) / selectedUnits.Length;

            var size = PotetialFieldsHelper.PpSize;
            PotetialFieldsHelper.Clear();
            PotetialFieldsHelper.FillBaseWorldPower();
            PotetialFieldsHelper.AppendEnemyPower();
            PotetialFieldsHelper.AppendAllyFlyingPowerForFlyingUnits(selectedUnits);
            PotetialFieldsHelper.Normalize();
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var color = PotetialFieldsHelper.GetColorFromValue(PotetialFieldsHelper.PotentialFields[i, j]);

                    rewindClient.Rectangle(i * size, j * size, (i + 1) * size, (j + 1) * size, color);
                }
            }

            rewindClient.End();

            var nextPpPoint = PotetialFieldsHelper.GetNextSafest_PP_PointByWorldXY(cx, cy);
            rewindClient.Rectangle(nextPpPoint.X * size, nextPpPoint.Y * size, (nextPpPoint.X + 1) * size, (nextPpPoint.Y + 1) * size, Color.Black);

            if (world.TickIndex % 6 == 0)
            {
                var vx = nextPpPoint.X * size + size/2d - cx;
                var vy = nextPpPoint.Y * size + size/2d - cy;
                ActionHelper.Move(vx, vy);

                Console.WriteLine($"vx {vx}\tvy {vy}\tRemainingActionCooldownTicks {me.RemainingActionCooldownTicks}");
            }
            return;
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
    }
}