using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class PotetialFieldsHelper
    {
        public static int Size = 32;
        public static float[,] PotentialFields = new float[Size, Size];

        public static void Clear()
        {
            PotentialFields = new float[Size, Size];
        }

        public static void FillBaseWorldPower()
        {
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    var val = (i / (float)Size + j / (float)Size) / 2;
                    PotentialFields[i, j] = (1 - val) * 10000;
                }
            }
        }

        public static void AppendEnemyPower()
        {
            var enemies = UnitHelper.Units.Values.Where(x => x.Side == Side.Enemy).ToArray();
            foreach(var enemy in enemies)
            {
                var cellX = (int)enemy.X / Size;
                var cellY = (int)enemy.Y / Size;

                PotentialFields[cellX, cellY] += 100;

                if (cellX - 1 > 0)
                {
                    PotentialFields[cellX - 1, cellY] += 100;
                }

                if (cellX + 1 < Size)
                {
                    PotentialFields[cellX + 1, cellY] += 100;
                }

                if (cellY - 1 > 0)
                {
                    PotentialFields[cellX, cellY - 1] += 100;
                }

                if (cellY + 1 < Size)
                {
                    PotentialFields[cellX, cellY + 1] += 100;
                }
            }
        }

        public static void Normalize()
        {
            var maxPower = (from float x in PotentialFields select x).Max();
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    PotentialFields[i, j] = PotentialFields[i, j] / maxPower;
                }
            }
        }

        public static Color GetColorFromValue(float fieldValue)
        {
            return Color.FromArgb(100, (int)(fieldValue * 255), (int)(255 - fieldValue * 255), 0);
        }
    }
}
