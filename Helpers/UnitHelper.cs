﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class UnitHelper
    {
        public static Dictionary<long, MyLivingUnit> Units { get; set; } = new Dictionary<long, MyLivingUnit>(1000);
    }

    public class MyLivingUnit
    {
        public long Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public Side Side { get; set; }
        public int Durability { get; set; }
        public int MaxDurability { get; set; }
        public VehicleType Type { get; set; }
    }

    public enum Side
    {
        Our = -1,
        Neutral = 0,
        Enemy = 1
    }
}
