using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class BattleHelper
    {
        public static CalculatePowerResult CalculatePower(List<DbScanHelper.Point> enemies, VehicleType currentVehicleType, int basePower)
        {
                var enemyPower = 0F;

                if (currentVehicleType == VehicleType.Fighter)
                {
                    foreach (var enemy in enemies)
                    {
                        var power = (float) basePower;

                        if (enemy.Type == VehicleType.Fighter)
                        {
                            power = basePower * 1;
                        }
                        else if (enemy.Type == VehicleType.Helicopter)
                        {
                            power = basePower * 0.33F;
                        }
                        else if (enemy.Type == VehicleType.Ifv)
                        {
                            power = basePower * 0.4F;
                        }
                        else if (enemy.Type == VehicleType.Tank)
                        {
                            power = basePower * 0;
                        }
                        else if (enemy.Type == VehicleType.Arrv)
                        {
                            power = basePower * 0;
                        }

                        power *= GetPowerHealthMulitplier(enemy.Type, enemy.Durability);

                        enemyPower += power;
                    }
                }
                else if (currentVehicleType == VehicleType.Helicopter)
                {
                    foreach (var enemy in enemies)
                    {
                        var power = (float) basePower;

                        if (enemy.Type == VehicleType.Fighter)
                        {
                            power = basePower * 3F;
                        }
                        else if (enemy.Type == VehicleType.Helicopter)
                        {
                            power = basePower * 1;
                        }
                        else if (enemy.Type == VehicleType.Ifv)
                        {
                            power = basePower * 1.5F;
                        }
                        else if (enemy.Type == VehicleType.Tank)
                        {
                            power = basePower * 0.6F;
                        }
                        else if (enemy.Type == VehicleType.Arrv)
                        {
                            power = basePower * 0F;
                        }

                        power *= GetPowerHealthMulitplier(enemy.Type, enemy.Durability);

                        enemyPower += power;
                    }
                }
                else if (currentVehicleType == VehicleType.Tank)
                {
                    foreach (var enemy in enemies)
                    {
                        var power = (float) basePower;

                        if (enemy.Type == VehicleType.Fighter)
                        {
                            power = basePower * 0;
                        }
                        else if (enemy.Type == VehicleType.Helicopter)
                        {
                            power = basePower * 1.4F;
                        }
                        else if (enemy.Type == VehicleType.Ifv)
                        {
                            power = basePower * 0.6F;
                        }
                        else if (enemy.Type == VehicleType.Tank)
                        {
                            power = basePower * 1;
                        }
                        else if (enemy.Type == VehicleType.Arrv)
                        {
                            power = basePower * 0F;
                        }

                        power *= GetPowerHealthMulitplier(enemy.Type, enemy.Durability);

                        enemyPower += power;
                    }
                }
                else if (currentVehicleType == VehicleType.Ifv)
                {
                    foreach (var enemy in enemies)
                    {
                        var power = (float) basePower;

                        if (enemy.Type == VehicleType.Fighter)
                        {
                            power = basePower * 0.01F;
                        }
                        else if (enemy.Type == VehicleType.Helicopter)
                        {
                            power = basePower * 0.66F;
                        }
                        else if (enemy.Type == VehicleType.Ifv)
                        {
                            power = basePower * 1;
                        }
                        else if (enemy.Type == VehicleType.Tank)
                        {
                            power = basePower * 1.5F;
                        }
                        else if (enemy.Type == VehicleType.Arrv)
                        {
                            power = basePower * 0F;
                        }

                        power *= GetPowerHealthMulitplier(enemy.Type, enemy.Durability);

                        enemyPower += power;
                    }
                }
                else if (currentVehicleType == VehicleType.Arrv)
                {
                    foreach (var enemy in enemies)
                    {
                        var power = (float) basePower;

                        if (enemy.Type == VehicleType.Fighter)
                        {
                            power = basePower * 0;
                        }
                        else if (enemy.Type == VehicleType.Helicopter)
                        {
                            power = basePower * 1;
                        }
                        else if (enemy.Type == VehicleType.Ifv)
                        {
                            power = basePower * 1;
                        }
                        else if (enemy.Type == VehicleType.Tank)
                        {
                            power = basePower * 1;
                        }
                        else if (enemy.Type == VehicleType.Arrv)
                        {
                            power = basePower * 0; //Чтобы не сталкиваться
                        }

                        power *= GetPowerHealthMulitplier(enemy.Type, enemy.Durability);

                        enemyPower += power;
                    }
                }

            var canAttackSomeone = enemies.Any(x => GetUnitTypesThisTypeCanAttack(currentVehicleType).Contains(x.Type));

            return new CalculatePowerResult(enemyPower, canAttackSomeone);
        }

        /// <summary>
        /// Вернуть список юнитов, которых может атаковать переданный тип
        /// </summary>
        /// <param name="vehicleType"></param>
        /// <returns></returns>
        private static VehicleType[] GetUnitTypesThisTypeCanAttack(VehicleType vehicleType)
        {
            if (vehicleType == VehicleType.Fighter)
            {
                return new[] { VehicleType.Fighter, VehicleType.Helicopter };
            }
            else if (vehicleType == VehicleType.Helicopter)
            {
                return new[] { VehicleType.Fighter, VehicleType.Helicopter, VehicleType.Ifv, VehicleType.Tank, VehicleType.Arrv };
            }
            else if (vehicleType == VehicleType.Tank)
            {
                return new[] { VehicleType.Helicopter, VehicleType.Ifv, VehicleType.Tank, VehicleType.Arrv };
            }
            else if (vehicleType == VehicleType.Ifv)
            {
                return new[] { VehicleType.Fighter, VehicleType.Helicopter, VehicleType.Ifv, VehicleType.Tank, VehicleType.Arrv };
            }
            else if (vehicleType == VehicleType.Arrv)
            {
                return new VehicleType[0];
            }
            throw new NotImplementedException();
        }

        public static float GetPowerHealthMulitplier(VehicleType vehicleType, int health)
        {
            if (vehicleType == VehicleType.Fighter)
            {
                return (float)health / GlobalHelper.Game.FighterDurability;
            }
            else if (vehicleType == VehicleType.Helicopter)
            {
                return (float)health / GlobalHelper.Game.HelicopterDurability;
            }
            else if (vehicleType == VehicleType.Tank)
            {
                return (float)health / GlobalHelper.Game.TankDurability;
            }
            else if (vehicleType == VehicleType.Ifv)
            {
                return (float)health / GlobalHelper.Game.IfvDurability;
            }
            else if (vehicleType == VehicleType.Arrv)
            {
                return (float)health / GlobalHelper.Game.ArrvDurability;
            }

            throw new NotImplementedException();
        }
    }

    public class CalculatePowerResult
    {
        public float EnemyPower { get; }
        public bool CanAttackSomeone { get; }

        public CalculatePowerResult(float enemyPower, bool canAttackSomeone)
        {
            EnemyPower = enemyPower;
            CanAttackSomeone = canAttackSomeone;
        }
    }
}
