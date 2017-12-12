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
        public static GameMode Mode => Game.IsFogOfWarEnabled ? GameMode.FacFow : GameMode.FacNoFow;
        public static bool MoveAllowed => Me.RemainingActionCooldownTicks == 0;

        public static Exception GetException(string message)
        {
            //Такого типа потому что так видно в логах на сайте
            return new IndexOutOfRangeException(message);
        }
    }

    public enum GameMode
    {
        /// <summary>
        /// Без зданий, без тумана
        /// </summary>
        NoFacNoFow,
        
        /// <summary>
        /// Здания, без тумана
        /// </summary>
        FacNoFow,

        /// <summary>
        /// Здания , с туманом
        /// </summary>
        FacFow
    }
}
