using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public class ConfigurationHelper
    {
        public static bool EnableNuclearStrike = true;

        public static int MovesCoutToScale = 15;

        public static bool FacilityCreateGroupEnabled = true;

        /// <summary>
        /// Коэфициент, колько процентов вражеских юнитов должно быть под атакой 
        /// от общего кол-ва вражеских юнитов,
        /// если меньше, не атакуем,
        /// Значение от 0 до 1;
        /// </summary>
        public static double NuclearStrikeTargetEnemiesCoef = 0.03;
    }
}
