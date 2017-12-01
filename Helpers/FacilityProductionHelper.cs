using System.Collections.Generic;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;
using System.Linq;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class FacilityProductionHelper
    {
        public static List<FacilityEx> FacilitiesToAddProdution { get; set; } = new List<FacilityEx>();

        public static List<FacilityEx> FacilitiesToCreateGroup { get; set; } = new List<FacilityEx>();

        public static void StartFactoryProduction(FacilityEx facility)
        {
            var productionType = GetVehicleTypeToStartProduction();
            facility.LastAssignedVehicleType = productionType;
            ActionHelper.StartFactoryProduction(facility.Id, productionType);
        }

        private static VehicleType GetVehicleTypeToStartProduction()
        {
            var vehicleType = VehicleType.Tank;

            var alliesCanProduct = UnitHelper.UnitsAlly.Where(x => x.Type != VehicleType.Arrv).ToArray();

            if (alliesCanProduct.Length > 0)
            {
                var aliesByType = alliesCanProduct.GroupBy(x => x.Type).Select(x => new
                {
                    X = x.Key,
                    Count = x.Count()
                }).ToList();

                var minAllyCount = aliesByType.Min(x => x.Count);
                var minAlly = aliesByType.First(x => x.Count == minAllyCount);
                vehicleType = minAlly.X;
            }

            return vehicleType;
        }

    }
}
