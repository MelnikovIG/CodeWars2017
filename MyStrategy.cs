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

            var size = PotetialFieldsHelper.Size;
            PotetialFieldsHelper.Clear();
            PotetialFieldsHelper.FillBaseWorldPower();
            PotetialFieldsHelper.AppendEnemyPower();
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

            //var groupItems = world.VehicleUpdates.Where(x => x.Groups.Contains(1)).ToList();

            //if (groupItems.Count > 0)
            //{
            //    var right = groupItems.Max(x => x.X);
            //    var left = groupItems.Min(x => x.X);
            //    var top = groupItems.Max(x => x.Y);
            //    var bot = groupItems.Min(x => x.Y);

            //    var centerX = right + left / 2;
            //    var centerY = top + bot / 2;
            //    Console.WriteLine($"X: {centerX}\t Y: {centerY}");
            //}
            //else
            //{
            //    Console.WriteLine($"X: - \t Y: -");
            //}

            if (world.TickIndex == 0)
            {
                ActionHelper.Select(0, 0, world.Width, world.Height, VehicleType.Fighter);
                return;
            }

            if (world.TickIndex == 1)
            {
                ActionHelper.SetSelectedGroup(1);
                return;
            }

            if (world.TickIndex == 2)
            {
                ActionHelper.Move(world.Width / 2.0D, world.Height / 2.0D / 2.0D);
                return;
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
    }
}