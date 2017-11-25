using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class GlobalHelper
    {
        public static World World { get; set; }
        public static Move Move { get; set; }
        public static Game Game { get; set; }
        public static Player Me { get; set; }
        public static Player Enemy { get; set; }

        public static bool MoveAllowed => Me.RemainingActionCooldownTicks == 0;
    }
}
