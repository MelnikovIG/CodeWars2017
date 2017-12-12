﻿using System;
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

        /// <summary>
        /// Количество тиков , после которых нужно перепроверить вражеское/нейтральное здание
        /// </summary>
        public static int TicksCountToRecheckFacility = 1000;


        private const double RecheckFacilityDistanse = 70;
        /// <summary>
        /// Дистанция до цента здания от центра группы, когда оно считается проверенным
        /// </summary>
        public static double RecheckFacilityDistansePow2 = RecheckFacilityDistanse * RecheckFacilityDistanse;
    }
}
