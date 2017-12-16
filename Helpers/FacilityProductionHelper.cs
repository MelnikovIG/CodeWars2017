using System;
using System.Collections.Generic;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;
using System.Linq;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class FacilityProductionHelper
    {
        public static List<FacilityEx> FacilitiesToAddProdution { get; set; } = new List<FacilityEx>();

        public static List<FacilityEx> FacilitiesToCreateGroup { get; set; } = new List<FacilityEx>();

        //public const int FacilityUnitInRow = 11;
        //public const int MaxCountToCreate = FacilityUnitInRow * FacilityUnitInRow;
        public const int MaxCountToCreate = 33;

        public static void StartFactoryProduction(FacilityEx facility, List<List<DbScanHelper.Point>> clusters)
        {
            var productionParams = GetStartProductionParams(clusters);
            facility.LastAssignedVehicleType = productionParams.VehicleType;
            facility.ProductionCount = productionParams.Count;
            facility.FacilityGroupCreating = false;
            ActionHelper.StartFactoryProduction(facility.Id, productionParams.VehicleType);
        }

        public static void StopFactoryProduction(FacilityEx facility)
        {
            facility.LastAssignedVehicleType = null;
            facility.ProductionCount =  0;
            facility.FacilityGroupCreating = false;
            ActionHelper.StopFactoryProduction(facility.Id);
        }


        private static StartProductionParams GetStartProductionParams(List<List<DbScanHelper.Point>> clusters)
        {
            if (clusters.Count == 0)
            {
                return new StartProductionParams(VehicleType.Tank, MaxCountToCreate);
            }

            var clustersOrder = clusters.OrderByDescending(x => x.Count).ToList();

            var productionVehicleTypes = new[]
            {
                VehicleType.Tank,
                VehicleType.Ifv,
                VehicleType.Helicopter,
                VehicleType.Fighter
            };

            var basePower = PotentialFieldsHelper.EnemyPowerToDodge;

            foreach (var cluster in clustersOrder)
            {
                foreach (var productionVehicleType in productionVehicleTypes)
                {
                    var res = BattleHelper.CalculatePower(cluster, productionVehicleType, basePower);
                    if (!res.CanAttackSomeone) 
                        continue;

                    //Если уже производится отряд сильнее или есть, то не делаем еще раз такойже
                    var currentGroupsOfType = GroupHelper.Groups.Where(x => x.VehicleType == productionVehicleType)
                        .ToArray();

                    var anyExistGroupStronger = currentGroupsOfType.Any(x =>
                    {
                        var groupUnits = UnitHelper.UnitsAlly.Where(y => y.Groups.Contains(x.Id)).ToArray();
                        var existGroupPower =
                            groupUnits.Sum(y => BattleHelper.GetPowerHealthMulitplier(x.VehicleType, y.Durability)) *
                            basePower;
                        return existGroupPower > res.EnemyPower;
                    });

                    if (anyExistGroupStronger)
                    {
                        continue;
                    }

                    var producingFactories = FacilityHelper.Facilities.Select(x => x.Value)
                        .Where(x => x.Side == Side.Our)
                        .Where(x => x.Type == FacilityType.VehicleFactory)
                        .Where(x => x.VehicleType != null && x.VehicleType == productionVehicleType)
                        .ToArray();

                    var isAnyProducingStronger = producingFactories.Any(x =>
                    {
                        var facPower = x.ProductionCount * basePower;
                        return facPower > res.EnemyPower;
                    });

                    if (isAnyProducingStronger)
                    {
                        continue;
                    }

                    //Нельзя произвести юнитов сильнее отряда
                    if (res.EnemyPower > MaxCountToCreate * basePower)
                    {
                        continue;
                    }

                    if (Math.Abs(res.EnemyPower) < PotentialFieldsHelper.Epsilon)
                    {
                        return new StartProductionParams(productionVehicleType, MaxCountToCreate/2);
                    }

                    var powerToCreate = res.EnemyPower * 1.2;
                    var count = (int)(powerToCreate / basePower);

                    if (count > MaxCountToCreate)
                    {
                        count = MaxCountToCreate;
                    }

                    return new StartProductionParams(productionVehicleType, count);
                }
            }

            return new StartProductionParams(VehicleType.Tank, MaxCountToCreate);
        }
    }

    public class StartProductionParams
    {
        public VehicleType VehicleType { get; }
        public int Count { get; }

        public StartProductionParams(VehicleType vehicleType, int count)
        {
            VehicleType = vehicleType;
            Count = count;
        }
    }
}
