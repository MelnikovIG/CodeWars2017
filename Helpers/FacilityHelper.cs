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

        //Поможет в случае потери завода
        public VehicleType? LastAssignedVehicleType { get; set; }
        //Кол-во юнитов для производства
        public int ProductionCount { get; set; }

        /// <summary>
        /// В какой тик здание последний раз было видимым
        /// </summary>
        public int LastVisitedTick { get; set; } = -ConfigurationHelper.TicksCountToRecheckFacility;

        /// <summary>
        /// Сколько тиков прошло с моменты последнего визита
        /// </summary>
        public int LastVisitTicksAgo => GlobalHelper.World.TickIndex - LastVisitedTick;

        public bool FacilityGroupCreating { get; set; } = false;
        public bool ProductionInProgress { get; set; }
    }

    public static class FacilityHelper
    {
        public static Dictionary<long, FacilityEx> Facilities =  new Dictionary<long, FacilityEx>();
        public static FacilityEx[] MyFacilities = Facilities.Select(x => x.Value).Where(x => x.Side == Side.Our).ToArray();
        public static FacilityEx[] NotMyFacilities = Facilities.Select(x => x.Value).Where(x => x.Side != Side.Our).ToArray();

        public static void UpdateFacilitiesStates()
        {
            var facilityWidth = GlobalHelper.Game.FacilityWidth;
            var facilityHeight = GlobalHelper.Game.FacilityHeight;

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

                if (facility.Type == FacilityType.VehicleFactory)
                {
                    var needStopProduction = NeedStopProduction();

                    //Если захватили здание
                    if (gotMineThisTick)
                    {
                        //И можно стартовать производствао
                        if (needStopProduction)
                        {
                            facility.ProductionInProgress = false;
                            //Не будем стопать, так как только что захватили
                            //QueueHelper.Queue.Enqueue(new StopProduction(facility));
                        }
                        else
                        {
                            facility.ProductionInProgress = true;
                            QueueHelper.Queue.Enqueue(new StartProduction(facility));
                        }
                    }
                    //Если производство было остановлено и можно запускать заного
                    else if (!facility.ProductionInProgress && !needStopProduction && facility.Side == Side.Our)
                    {
                        facility.ProductionInProgress = true;
                        QueueHelper.Queue.Enqueue(new StartProduction(facility));
                    }
                    else
                    {
                        var createdUnassignedUnits = facility.GetCreatedUnassignedUnits();
                        var needCreateGroupFromProducingUnits =
                            lostMineThisTick ||
                            (facility.ProductionCount > 0 && createdUnassignedUnits.Length >= facility.ProductionCount);

                        if (needCreateGroupFromProducingUnits)
                        {
                            if (!facility.FacilityGroupCreating)
                            {
                                facility.FacilityGroupCreating = true;
                                QueueHelper.Queue.Enqueue(new SelectUnits(facility.Left, facility.Top,
                                    facility.Left + facilityWidth, facility.Top + facilityHeight,
                                    facility.LastAssignedVehicleType));

                                var vehicleType = facility.LastAssignedVehicleType ?? VehicleType.Tank;

                                QueueHelper.Queue.Enqueue(new AddSelecteUnitsToNewGroupTask(vehicleType));

                                if (needStopProduction)
                                {
                                    facility.ProductionInProgress = false;
                                    QueueHelper.Queue.Enqueue(new StopProduction(facility));
                                }
                                else
                                {
                                    facility.ProductionInProgress = true;
                                    QueueHelper.Queue.Enqueue(new StartProduction(facility));
                                }
                            }
                        }
                        else if(needStopProduction && facility.Side == Side.Our && facility.ProductionInProgress)
                        {
                            if (createdUnassignedUnits.Length > 0)
                            {
                                QueueHelper.Queue.Enqueue(new SelectUnits(facility.Left, facility.Top,
                                    facility.Left + facilityWidth, facility.Top + facilityHeight,
                                    facility.LastAssignedVehicleType));

                                var vehicleType = facility.LastAssignedVehicleType ?? VehicleType.Tank;

                                QueueHelper.Queue.Enqueue(new AddSelecteUnitsToNewGroupTask(vehicleType));
                            }

                            facility.ProductionInProgress = false;
                            QueueHelper.Queue.Enqueue(new StopProduction(facility));
                        }
                    }

                }
            }

            if (GlobalHelper.Mode == GameMode.FacFow)
            {
                foreach (var myGroup in GroupHelper.Groups)
                {
                    var groupUnits = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains(myGroup.Id)).ToArray();
                    var xCenter = groupUnits.Sum(x => x.X) / groupUnits.Length;
                    var yCenter = groupUnits.Sum(x => x.Y) / groupUnits.Length;

                    foreach (var facility in FacilityHelper.Facilities.Values)
                    {
                        if (facility.Side == Side.Our)
                        {
                            facility.LastVisitedTick = GlobalHelper.World.TickIndex;
                        }
                        else
                        {
                            var fx = facility.Left + GlobalHelper.Game.FacilityWidth / 2;
                            var fy = facility.Top + GlobalHelper.Game.FacilityHeight / 2;

                            var currentDistance = GeometryHelper.GetDistancePower2To(xCenter, yCenter, fx, fy);
                            var recheckDistancePow2 = ConfigurationHelper.RecheckFacilityDistansePow2;

                            if (currentDistance <= recheckDistancePow2)
                            {
                                facility.LastVisitedTick = GlobalHelper.World.TickIndex;
                            }
                        }
                    }
                }
            }
        }

        private static bool NeedStopProduction()
        {
            var isProductionTickExceed = GlobalHelper.Game.TickCount - GlobalHelper.World.TickIndex <
                                       ConfigurationHelper.StopProductionWhenTicksToEndGameRemaining;

            //var testStopProduction = (GlobalHelper.World.TickIndex / 1000) % 2 == 1;

            //Остановим производство, если слишком много юнитов
            //Для варианта с туманом, если их больше n
            //Для варианта без тумана, если количество превышает кол-во врага в n раз
            var isUnitsTooMuch = (GlobalHelper.Mode == GameMode.FacFow
                                     ? UnitHelper.UnitsAlly.Length > 750
                                     : UnitHelper.UnitsAlly.Length - UnitHelper.UnitsEnemy.Length > 300)
                                 && GlobalHelper.World.TickIndex > 10000;

            var isEnemyCanBeatProducingGroupTooClose = false;

            var result = isProductionTickExceed || isUnitsTooMuch || isEnemyCanBeatProducingGroupTooClose/*|| testStopProduction*/;

            if (result)
            {
                var a = 0;
            }

            return result;
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

                    if (facility.VehicleType != null)
                    {
                        var progressRange = facilityWidth * facility.ProductionProgress /
                                            GetProdutionTicksForType(facility.VehicleType.Value);
                        rewindClient.Rectangle(
                            facility.Left,
                            facility.Top + facilityHeight + 10,
                            facility.Left + progressRange,
                            facility.Top + facilityHeight + 20,
                            Color.Green);
                    }
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

                    if (facility.VehicleType != null)
                    {
                        var progressRange = facilityWidth * facility.ProductionProgress /
                                            GetProdutionTicksForType(facility.VehicleType.Value);
                        rewindClient.Rectangle(
                            facility.Left,
                            facility.Top + facilityHeight + 10,
                            facility.Left + progressRange,
                            facility.Top + facilityHeight + 20,
                            Color.Red);
                    }
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

                if (facility.LastVisitTicksAgo >= ConfigurationHelper.TicksCountToRecheckFacility)
                {
                    rewindClient.Rectangle(
                        facility.Left,
                        facility.Top,
                        facility.Left + facilityWidth,
                        facility.Top + facilityHeight,
                        Color.FromArgb(150, 0, 0, 0));
                }
            }
#endif
        }

        private static int GetProdutionTicksForType(VehicleType vehicleType)
        {
            if (vehicleType == VehicleType.Fighter)
            {
                return GlobalHelper.Game.FighterProductionCost;
            }
            if (vehicleType == VehicleType.Helicopter)
            {
                return GlobalHelper.Game.HelicopterProductionCost;
            }
            if (vehicleType == VehicleType.Tank)
            {
                return GlobalHelper.Game.TankProductionCost;
            }
            if (vehicleType == VehicleType.Ifv)
            {
                return GlobalHelper.Game.IfvProductionCost;
            }
            if (vehicleType == VehicleType.Arrv)
            {
                return GlobalHelper.Game.ArrvProductionCost;
            }

            throw new NotImplementedException();
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
