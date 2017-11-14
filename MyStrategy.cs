using System;
using System.Drawing;
using System.Linq;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk {
    public sealed class MyStrategy : IStrategy {
        public void Move(Player me, World world, Game game, Move move) {
            GlobalHelper.World = world;
            GlobalHelper.Move = move;

            var rewindClient = RewindClient.RewindClient.Instance;
            rewindClient.Circle(0,0,100,Color.Black);

            foreach(var newVehicle in world.NewVehicles){
                UnitHelper.Units.Add(newVehicle.Id, new MyLivingUnit()
                {
                    Id = newVehicle.Id,
                    X = newVehicle.X,
                    Y = newVehicle.Y
                });
            }


            foreach (var vehicleUpdate in world.VehicleUpdates)
            {
                var vehicle = UnitHelper.Units[vehicleUpdate.Id];
                vehicle.X = vehicleUpdate.X;
                vehicle.Y = vehicleUpdate.Y;
            }

            foreach(var unit in UnitHelper.Units.Values)
            {
                //rewindClient.Circle(vehicleUpdate.X, vehicleUpdate.Y, game.VehicleRadius, Color.Red);
                rewindClient.LivingUnit(unit.X, unit.Y, game.VehicleRadius, 0, 0, RewindClient.Side.Enemy);
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
                ActionHelper.Select(0, 0, world.Width, world.Height, VehicleType.Ifv);
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
    }
}