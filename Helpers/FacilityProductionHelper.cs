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

        public static int FacilityUnitInRow { get; } = 11;
        public static int MaxCountToCreate { get; } = FacilityUnitInRow * FacilityUnitInRow;

        public static void StartFactoryProduction(FacilityEx facility, List<List<DbScanHelper.Point>> clusters)
        {
            var productionParams = GetStartProductionParams(clusters);
            facility.LastAssignedVehicleType = productionParams.VehicleType;
            facility.ProductionCount = productionParams.Count;
            ActionHelper.StartFactoryProduction(facility.Id, productionParams.VehicleType);
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
