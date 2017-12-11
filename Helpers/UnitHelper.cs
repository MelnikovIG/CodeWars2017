using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;
using RewindClient;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class UnitHelper
    {
        public static Dictionary<long, MyLivingUnit> Units { get; set; } = new Dictionary<long, MyLivingUnit>(1000);
        //TODO: cacheGetForTick
        public static MyLivingUnit[] UnitsAlly => Units.Select(x => x.Value).Where(x => x.Side == Side.Our).ToArray();
        //TODO: cacheGetForTick
        public static MyLivingUnit[] UnitsEnemy => Units.Select(x => x.Value).Where(x => x.Side == Side.Enemy).ToArray();

        public static void DrawAllUnits()
        {
#if DEBUG
            var rewindClient = RewindClient.RewindClient.Instance;
            foreach (var unit in UnitHelper.Units.Values)
            {

                rewindClient.LivingUnit(
                    unit.X,
                    unit.Y,
                    GlobalHelper.Game.VehicleRadius,
                    unit.Durability,
                    unit.MaxDurability,
                    (RewindClient.Side)unit.Side,
                    0,
                    GetRewindClientUnitType(unit.Type));

                if (unit.IsSelected)
                {
                    rewindClient.Circle(unit.X,
                        unit.Y,
                        GlobalHelper.Game.VehicleRadius * 3,
                        Color.FromArgb(200, 255, 255, 0));
                }


                if (unit.Side == Side.Our)
                {
                    rewindClient.Circle(unit.X,
                        unit.Y,
                        NuclearStrikeHelper.GetVisionRangeByWeather(unit),
                        Color.FromArgb(5, 0, 0, 255),
                        1);
                }
            }
#endif
        }

        private static RewindClient.UnitType GetRewindClientUnitType(VehicleType vehicleType)
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

    public class MyLivingUnit
    {
        public long Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Radius { get; set; }
        public Side Side { get; set; }
        public int Durability { get; set; }
        public int MaxDurability { get; set; }
        public VehicleType Type { get; set; }
        public int[] Groups { get; set; } = new int[0];
        public bool IsSelected { get; set; }
    }

    public enum Side
    {
        Our = -1,
        Neutral = 0,
        Enemy = 1
    }
}
