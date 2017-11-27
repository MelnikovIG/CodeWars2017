using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public class FacilityEx
    {
        public long Id { get; set; }
        //public long OwnerPlayerId { get; set; }
        //public int ProductionProgress { get; set; }
        public Side Side { get; set; }
        public FacilityType Type { get; set; }
        public double CapturePoints { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }

        public bool GotMineThisTick { get; set; }
        public bool LostMineThisTick { get; set; }
        public int ProductionProgress { get; set; }
        public VehicleType? VehicleType { get; set; }
    }

    public static class FacilityHelper
    {
        public static Dictionary<long, FacilityEx> Facilities =  new Dictionary<long, FacilityEx>();
        public static FacilityEx[] MyFacilities = Facilities.Select(x => x.Value).Where(x => x.Side == Side.Our).ToArray();
        public static FacilityEx[] EnemyFacilities = Facilities.Select(x => x.Value).Where(x => x.Side == Side.Enemy).ToArray();

        public static void UpdateFacilitiesStates()
        {
            var worldFacilities = GlobalHelper.World.Facilities;

            foreach (var worldFacility in worldFacilities)
            {
                if (!FacilityHelper.Facilities.ContainsKey(worldFacility.Id))
                {
                    FacilityHelper.Facilities.Add(worldFacility.Id, new FacilityEx());
                }

                var side = Side.Neutral;
                if (worldFacility.OwnerPlayerId == GlobalHelper.Me.Id)
                {
                    side = Side.Our;
                }
                else if (worldFacility.OwnerPlayerId == GlobalHelper.Enemy.Id)
                {
                    side = Side.Enemy;
                }

                var facility = FacilityHelper.Facilities[worldFacility.Id];

                var gotMineThisTick = facility.Side != Side.Our && side == Side.Our;
                var lostMineThisTick = facility.Side == Side.Our && side != Side.Our;

                facility.Id = worldFacility.Id;
                //facility.OwnerPlayerId = worldFacility.OwnerPlayerId;
                facility.VehicleType = worldFacility.VehicleType;
                facility.ProductionProgress = worldFacility.ProductionProgress;
                facility.Type = worldFacility.Type;
                facility.CapturePoints = worldFacility.CapturePoints;
                facility.Left = worldFacility.Left;
                facility.Top = worldFacility.Top;
                facility.Side = side;
                facility.GotMineThisTick = gotMineThisTick;
                facility.LostMineThisTick = lostMineThisTick;

                var facilitiesToAddProdution = FacilityProductionHelper.FacilitiesToAddProdution;
                if (facility.Type == FacilityType.VehicleFactory)
                {
                    if (gotMineThisTick)
                    {
                        if (!facilitiesToAddProdution.Contains(facility.Id))
                        {
                            facilitiesToAddProdution.Add(facility.Id);
                        }
                    }

                    if (lostMineThisTick)
                    {
                        if (facilitiesToAddProdution.Contains(facility.Id))
                        {
                            facilitiesToAddProdution.Remove(facility.Id);
                        }
                    }
                }
            }
        }

        public static void DrawFacilities()
        {
#if DEBUG
            var rewindClient = RewindClient.RewindClient.Instance;

            var facilityWidth = GlobalHelper.Game.FacilityWidth;
            var facilityHeight = GlobalHelper.Game.FacilityHeight;

            var myColor = Color.FromArgb(100, 0, 0, 255);
            var enemyColor = Color.FromArgb(100, 255, 0, 0);
            var neutralColor = Color.FromArgb(100, 105, 105, 105);

            foreach (var facility in FacilityHelper.Facilities.Values)
            {
                var color = neutralColor;
                if (facility.Side == Side.Our)
                {
                    color = myColor;
                }
                else if (facility.Side == Side.Enemy)
                {
                    color = enemyColor;
                }

                rewindClient.Rectangle(
                    facility.Left,
                    facility.Top,
                    facility.Left + facilityWidth,
                    facility.Top + facilityHeight,
                    color);

                if (facility.Type == FacilityType.ControlCenter)
                {
                    rewindClient.Circle(facility.Left + facilityWidth / 2, facility.Top + facilityHeight / 2, facilityWidth / 3, color);
                }
                else if (facility.Type == FacilityType.VehicleFactory)
                {
                    rewindClient.Rectangle(
                        facility.Left + facilityWidth * 0.25,
                        facility.Top + facilityHeight * 0.25,
                        facility.Left + facilityWidth * 0.75,
                        facility.Top + facilityHeight * 0.75,
                        color);
                }

                var maxFacilityCapturePoints = GlobalHelper.Game.MaxFacilityCapturePoints;

                if (facility.CapturePoints > 0)
                {
                    var captureFactor = facility.CapturePoints / maxFacilityCapturePoints;
                    var captureRange = facilityWidth * captureFactor;

                    rewindClient.Rectangle(
                        facility.Left,
                        facility.Top + facilityHeight,
                        facility.Left + captureRange,
                        facility.Top + facilityHeight + 10,
                        Color.Green);

                    var progressRange = facilityWidth * facility.ProductionProgress / 60;
                    rewindClient.Rectangle(
                        facility.Left,
                        facility.Top + facilityHeight + 10,
                        facility.Left + progressRange,
                        facility.Top + facilityHeight + 20,
                        Color.Green);
                }
                else if (facility.CapturePoints < 0)
                {
                    var captureFactor = facility.CapturePoints / maxFacilityCapturePoints;
                    var captureRange = -(facilityWidth * captureFactor);

                    rewindClient.Rectangle(
                        facility.Left,
                        facility.Top + facilityHeight,
                        facility.Left + captureRange,
                        facility.Top + facilityHeight + 10,
                        Color.Red);

                    var progressRange = facilityWidth * facility.ProductionProgress / 60;
                    rewindClient.Rectangle(
                        facility.Left,
                        facility.Top + facilityHeight + 10,
                        facility.Left + progressRange,
                        facility.Top + facilityHeight + 20,
                        Color.Red);
                }
                else
                {
                    rewindClient.Rectangle(
                        facility.Left,
                        facility.Top + facilityHeight,
                        facility.Left + facilityWidth,
                        facility.Top + facilityHeight + 10,
                        Color.DarkGray);
                }
            }
#endif
        }

        public static MyLivingUnit[] GetCreatedUnassignedUnits(this FacilityEx facilityEx)
        {
            if(facilityEx.Type == FacilityType.ControlCenter)
                return new MyLivingUnit[0];

            var facilityWidth = GlobalHelper.Game.FacilityWidth;
            var facilityHeight = GlobalHelper.Game.FacilityHeight;

            var createdUnassignedUnits = UnitHelper.UnitsAlly
                .Where(x => x.Groups.Length == 0)
                .Where(x =>
                    x.X >= facilityEx.Left &&
                    x.X <= facilityEx.Left + facilityWidth &&
                    x.Y >= facilityEx.Top &&
                    x.Y <= facilityEx.Top + facilityHeight
                ).ToArray();

            return createdUnassignedUnits;
        }
    }
}
