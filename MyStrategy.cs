using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk {
    public sealed class MyStrategy : IStrategy {
        public void Move(Player me, World world, Game game, Move move) {
            GlobalHelper.World = world;
            GlobalHelper.Move = move;

            if (world.TickIndex == 0)
            {
                ActionHelper.Select(0, 0, world.Width, world.Height,VehicleType.Ifv);
                return;
            }

            if (world.TickIndex == 1)
            {
                ActionHelper.Move(world.Width / 2.0D, world.Height / 2.0D / 2.0D);
                return;
            }
        }
    }
}