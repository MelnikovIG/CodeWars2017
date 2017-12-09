using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Custom;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;
#if DEBUG
using Newtonsoft.Json;
#endif
using RewindClient;
using Side = Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers.Side;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public sealed class MyStrategy : IStrategy
    {
        private static int PrepareStep = 0;
        private static bool Prepared = false;
        private static string EndOfString = " ";

        public void Move(Player me, World world, Game game, Move move)
        {
            var rewindClient = RewindClient.RewindClient.Instance;

#if DEBUG
            rewindClient.Message($"Queue length before move: {QueueHelper.Queue.Count}" + EndOfString);
            foreach (var queueItem in QueueHelper.Queue)
            {
                rewindClient.Message(queueItem.ToString() + EndOfString);
            }
            rewindClient.Message("-----------------------------------" + EndOfString);
#endif

            MoveEx(me, world, game, move, rewindClient);

#if DEBUG
            rewindClient.Message("-----------------------------------" + EndOfString);
            rewindClient.Message($"Queue length after move: {QueueHelper.Queue.Count}" + EndOfString);
            foreach (var queueItem in QueueHelper.Queue)
            {
                rewindClient.Message(queueItem.ToString() + EndOfString);
            }
            //var jsonMove = JsonConvert.SerializeObject(move);
            //rewindClient.Message(jsonMove);
#endif

            rewindClient.End();
        }

        public void MoveEx(Player me, World world, Game game, Move move, RewindClient.RewindClient rewindClient)
        {
            GlobalHelper.World = world;
            GlobalHelper.Move = move;
            GlobalHelper.Game = game;
            GlobalHelper.Me = me;
            GlobalHelper.Enemy = world.GetOpponentPlayer();

            var enemy = GlobalHelper.Enemy;

            UpdateVehiclesStates(me, world, game, rewindClient);
            FacilityHelper.UpdateFacilitiesStates();

            Lazy<List<List<DbScanHelper.Point>>> lazyClusters = new Lazy<List<List<DbScanHelper.Point>>>(() =>
            {
                var enemyPoints = UnitHelper.UnitsEnemy.Select(x => new DbScanHelper.Point(x.X, x.Y, x.Type, x.Durability)).ToList();
                List<List<DbScanHelper.Point>> clusters = DbScanHelper.GetClusters(enemyPoints, 15, 1);
                return clusters;
            });

#if DEBUG
            DbScanHelper.DrawClusters(lazyClusters.Value);
            FacilityHelper.DrawFacilities();
            NuclearStrikeHelper.DrawNuclearStrikes(me, enemy, game, rewindClient);
#endif

            if (!Prepared)
            {
                if (!GlobalHelper.MoveAllowed)
                {
                    return;
                }

                PrepareUnits();

                if (!Prepared)
                {
                    PrepareStep++;
                    return;
                }
            }

            if (QueueHelper.Queue.Count > 0)
            {
                if (!GlobalHelper.MoveAllowed)
                {
                    return;
                }

                var task = QueueHelper.Queue.Dequeue();

                if (task is AddSelecteUnitsToNewGroupTask)
                {
                    GroupHelper.CreateFroupForSelected((task as AddSelecteUnitsToNewGroupTask).VehicleType);
                    return;
                }
                else if (task is StartProduction)
                {
                    var sp = task as StartProduction;
                    FacilityProductionHelper.StartFactoryProduction(sp.Facility, lazyClusters.Value);
                    return;
                }
                else if (task is NuclearStrike)
                {
                    var ns = task as NuclearStrike;
                    ActionHelper.NuclearStrike(ns.VehicleId, ns.X, ns.Y);
                    return;
                }
                else if (task is SelectUnits)
                {
                    var sn = task as SelectUnits;
                    ActionHelper.Select(
                        sn.Left,
                        sn.Top,
                        sn.Right,
                        sn.Bottom,
                        sn.VehicleType);
                    return;
                }
                else if (task is Scale)
                {
                    var sc = task as Scale;
                    ActionHelper.Scale(sc.X, sc.Y, sc.Factor);
                    return;
                }
                else if (task is SelectGroup)
                {
                    var sG = task as SelectGroup;
                    ActionHelper.SelectGroup(sG.Group);
                    return;
                }
                else
                {
                    throw new NotImplementedException();
                }
                

                return;
            }

            var nucStrikeProcessed = NuclearStrikeHelper.ProcessEnemyNuclearStrikeDodge(GlobalHelper.MoveAllowed);
            if (nucStrikeProcessed)
            {
                return;
            }

            if (GlobalHelper.MoveAllowed)
            {
                var facilitiesToAddProdution = FacilityProductionHelper.FacilitiesToAddProdution;
                if (facilitiesToAddProdution.Count > 0)
                {
                    var facility = facilitiesToAddProdution[0];
                    facilitiesToAddProdution.Remove(facility);

                    FacilityProductionHelper.StartFactoryProduction(facility, lazyClusters.Value);
                    return;
                }

                var facilitiesToCreateGroup = FacilityProductionHelper.FacilitiesToCreateGroup;
                if (facilitiesToCreateGroup.Count > 0 && ConfigurationHelper.FacilityCreateGroupEnabled)
                {
                    var facility = facilitiesToCreateGroup[0];
                    facilitiesToCreateGroup.Remove(facility);

                    var facilityWidth = GlobalHelper.Game.FacilityWidth;
                    var facilityHeight = GlobalHelper.Game.FacilityHeight;

                    if (facility.LastAssignedVehicleType != null)
                    {
                        ActionHelper.Select(
                            facility.Left,
                            facility.Top,
                            facility.Left + facilityWidth,
                            facility.Top + facilityHeight,
                            facility.LastAssignedVehicleType);

                        QueueHelper.Queue.Enqueue(new AddSelecteUnitsToNewGroupTask(facility.LastAssignedVehicleType.Value));

                        QueueHelper.Queue.Enqueue(new StartProduction(facility));

                        return;
                    }
                }
            }

            var selectedUnits = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains(GroupHelper.CurrentGroup.Id)).ToArray();

            if (selectedUnits.Length == 0)
            {
                if (GlobalHelper.MoveAllowed)
                {
                    GroupHelper.SelectNextGroup();
                    return;
                }

                return;
            }

            if (GlobalHelper.MoveAllowed && ConfigurationHelper.EnableNuclearStrike)
            {
                if (me.NextNuclearStrikeTickIndex > 0)
                {
                    var isNucleatorInGroup = selectedUnits.Select(x => x.Id).Contains(me.NextNuclearStrikeVehicleId);
                    if (isNucleatorInGroup)
                    {
                        var nucleator = selectedUnits.FirstOrDefault(x => x.Id == me.NextNuclearStrikeVehicleId);
                        if (nucleator != null)
                        {
                            ActionHelper.StopMove();
                            return;
                        }
                    }
                }

                if (me.RemainingNuclearStrikeCooldownTicks <= 0)
                {
                    //var unit = UnitHelper.UnitsAlly.First();

                    //var vr = GetVisionRangeByWeather(unit);

                    //var maxRange =
                    //    UnitHelper.UnitsAlly
                    //        .Where(x => PotentialFieldsHelper.GetDistanceTo(x.X, x.Y, unit.X, unit.Y) < vr)
                    //        .Max(x => PotentialFieldsHelper.GetDistanceTo(x.X, x.Y, unit.X, unit.Y));

                    //var targetUnit =
                    //    UnitHelper.UnitsAlly.First(x => PotentialFieldsHelper.GetDistanceTo(x.X, x.Y, unit.X, unit.Y) == maxRange);

                    //ActionHelper.NuclearStrike(unit.Id, targetUnit.X, targetUnit.Y);
                    //return;

                    var hasTargetToNuclearAttack = NuclearStrikeHelper.HasTargetToNuclearAttack(selectedUnits);

                    if (hasTargetToNuclearAttack.Success)
                    {
                        //Остановимся для выстрела
                        ActionHelper.StopMove();


                        var selectedUnit = hasTargetToNuclearAttack.SelectedUnitRes;
                        var enemyUnit = hasTargetToNuclearAttack.EnemyRes;
                        QueueHelper.Queue.Enqueue(new NuclearStrike(selectedUnit.Id, enemyUnit.X, enemyUnit.Y));

                        return;
                    }
                }
            }

            var size = PotentialFieldsHelper.PpSize;
            PotentialFieldsHelper.Clear();
            if (me.RemainingNuclearStrikeCooldownTicks <= 0 && ConfigurationHelper.EnableNuclearStrike)
            {
                PotentialFieldsHelper.ApplyPowerToNuclearStrike();
            }
            else
            {
                PotentialFieldsHelper.AppendEnemyPowerToDodge(lazyClusters.Value);
                PotentialFieldsHelper.ApplyHealPower();
            }
            PotentialFieldsHelper.ApplyFacilitiesPower();
            PotentialFieldsHelper.AppendAllyUnitsToDodge(selectedUnits);
            PotentialFieldsHelper.Normalize();
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var color = PotentialFieldsHelper.GetColorFromValue(PotentialFieldsHelper.PotentialFields[i, j]);

                    rewindClient.Rectangle(i * size, j * size, (i + 1) * size, (j + 1) * size, color);
                }
            }

            if (GlobalHelper.MoveAllowed)
            {
                var currentSelectedGroup = GroupHelper.CurrentGroup;
                //Если на предыдущем ходу текущая группа уже двигалась, передадим управление след группе
                if (currentSelectedGroup.Moved)
                {
                    var isSelectSuccess = GroupHelper.SelectNextGroup();
                    if (isSelectSuccess)
                    {
                        currentSelectedGroup.Moved = false;
                        return;
                    }
                }

                if (currentSelectedGroup.MovesCount > 0 && currentSelectedGroup.MovesCount % ConfigurationHelper.MovesCoutToScale == 0)
                {
                    ScaleCurrentGroupToCenter();
                }
                else
                {
                    var cx = selectedUnits.Sum(x => x.X) / selectedUnits.Length;
                    var cy = selectedUnits.Sum(x => x.Y) / selectedUnits.Length;

                    var nextPpPoint = PotentialFieldsHelper.GetNextSafest_PP_PointByWorldXY(cx, cy);
                    var quartCellLength = PotentialFieldsHelper.PpSize * 0.25;
#if DEBUG
                    rewindClient.Rectangle(
                        nextPpPoint.X * size + quartCellLength,
                        nextPpPoint.Y * size + quartCellLength,
                        (nextPpPoint.X + 1) * size - quartCellLength,
                        (nextPpPoint.Y + 1) * size - quartCellLength,
                        Color.Black);
#endif

                    var nextPpPointX = nextPpPoint.X * size + size / 2d;
                    var nextPpPointY = nextPpPoint.Y * size + size / 2d;

                    //Хак по задержке вертов при старте, тянемся к ифвам
                    if (currentSelectedGroup.VehicleType == VehicleType.Helicopter && world.TickIndex < 1000)
                    {
                         var ifvGroup = GroupHelper.Groups.FirstOrDefault(x => x.VehicleType == VehicleType.Ifv);
                        if (ifvGroup != null)
                        {
                            var ifvUnits = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains(ifvGroup.Id)).ToArray();
                            var nextPpPointX1 = ifvUnits.Sum(x => x.X) / ifvUnits.Length / PotentialFieldsHelper.PpSize;
                            var nextPpPointY1 = ifvUnits.Sum(x => x.Y) / ifvUnits.Length / PotentialFieldsHelper.PpSize;
                            nextPpPoint = new Point2D(nextPpPointX1, nextPpPointY1);
                        }
                    }

                    //Если достаточно подойти к цели, отдадим управление дргому отряду
                    if (PotentialFieldsHelper.GetDistanceTo(nextPpPointX, nextPpPointY, cx, cy) < quartCellLength)
                    {
                        var isSelectSuccess = GroupHelper.SelectNextGroup();
                        if (isSelectSuccess)
                        {
                            currentSelectedGroup.Moved = false;
                            return;
                        }
                    }

                    var vx = nextPpPoint.X * size + size / 2d - cx;
                    var vy = nextPpPoint.Y * size + size / 2d - cy;
                    ActionHelper.Move(vx, vy);
                }

                currentSelectedGroup.Moved = true;
                currentSelectedGroup.MovesCount++;

                return;
            }
            return;
        }

        private RewindClient.UnitType GetRewindClientUitType(VehicleType vehicleType)
        {
            switch (vehicleType)
            {
                case VehicleType.Arrv: return UnitType.Arrv;
                case VehicleType.Fighter: return UnitType.Fighter;
                case VehicleType.Helicopter: return UnitType.Helicopter;
                case VehicleType.Ifv: return UnitType.Ifv;
                case VehicleType.Tank: return UnitType.Tank;
                default: return UnitType.Unknown;
            }
        }

        public void UpdateVehiclesStates(Player me, World world, Game game, RewindClient.RewindClient rewindClient)
        {
            var myId = me.Id;

            foreach (var newVehicle in world.NewVehicles)
            {
                UnitHelper.Units.Add(newVehicle.Id, new MyLivingUnit()
                {
                    Id = newVehicle.Id,
                    X = newVehicle.X,
                    Y = newVehicle.Y,
                    Radius = newVehicle.Radius,
                    Durability = newVehicle.Durability,
                    MaxDurability = newVehicle.MaxDurability,
                    Side = newVehicle.PlayerId == myId ? Side.Our : Side.Enemy,
                    IsSelected = newVehicle.IsSelected,
                    Type = newVehicle.Type
                });
            }

            foreach (var vehicleUpdate in world.VehicleUpdates)
            {
                var vehicle = UnitHelper.Units[vehicleUpdate.Id];
                vehicle.X = vehicleUpdate.X;
                vehicle.Y = vehicleUpdate.Y;
                vehicle.Durability = vehicleUpdate.Durability;
                vehicle.Groups = vehicleUpdate.Groups;
                vehicle.IsSelected = vehicleUpdate.IsSelected;
            }

            var emptyUnits = UnitHelper.Units.Where(x => x.Value.Durability <= 0).ToArray();
            if (emptyUnits.Length > 0)
            {
                foreach (var emptyUnit in emptyUnits){
                    UnitHelper.Units.Remove(emptyUnit.Key);
                }
            }


#if DEBUG
            foreach (var unit in UnitHelper.Units.Values)
            {

                rewindClient.LivingUnit(
                    unit.X,
                    unit.Y,
                    game.VehicleRadius,
                    unit.Durability,
                    unit.MaxDurability,
                    (RewindClient.Side) unit.Side,
                    0,
                    GetRewindClientUitType(unit.Type));

                if (unit.IsSelected)
                {
                    rewindClient.Circle(unit.X,
                        unit.Y,
                        game.VehicleRadius * 3,
                        Color.FromArgb(200, 255, 255, 0));
                }
            }
#endif
        }

        private static void PrepareUnits()
        {
            if (PrepareStep == 0)
            {
                SelectUnitsOfType(VehicleType.Fighter);
                return;
            }

            if (PrepareStep == 1)
            {
                GroupHelper.CreateFroupForSelected(VehicleType.Fighter);
                return;
            }

            if (PrepareStep == 2)
            {
                ScaleCurrentGroupToCenter();
                return;
            }

            if (PrepareStep == 3)
            {
                SelectUnitsOfType(VehicleType.Helicopter);
                return;
            }

            if (PrepareStep == 4)
            {
                GroupHelper.CreateFroupForSelected(VehicleType.Helicopter);
                return;
            }

            if (PrepareStep == 5)
            {
                ScaleCurrentGroupToCenter();
                return;
            }

            if (PrepareStep == 6)
            {
                SelectUnitsOfType(VehicleType.Tank);
                return;
            }

            if (PrepareStep == 7)
            {
                GroupHelper.CreateFroupForSelected(VehicleType.Tank);
                return;
            }

            if (PrepareStep == 8)
            {
                ScaleCurrentGroupToCenter();
                return;
            }

            if (PrepareStep == 9)
            {
                SelectUnitsOfType(VehicleType.Ifv);
                return;
            }

            if (PrepareStep == 10)
            {
                GroupHelper.CreateFroupForSelected(VehicleType.Ifv);
                return;
            }

            if (PrepareStep == 11)
            {
                ScaleCurrentGroupToCenter();
                return;
            }

            if (PrepareStep == 12)
            {
                SelectUnitsOfType(VehicleType.Arrv);
                return;
            }

            if (PrepareStep == 13)
            {
                GroupHelper.CreateFroupForSelected(VehicleType.Arrv);
                return;
            }

            if (PrepareStep == 14)
            {
                ScaleCurrentGroupToCenter();
                return;
            }

            if (PrepareStep == 15)
            {
                GroupHelper.SelectNextGroup();
            }

            if (PrepareStep == 16)
            {
                Prepared = true;
            }
        }

        private static void ScaleCurrentGroupToCenter()
        {
            var selectedUnitsForScale = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains(GroupHelper.CurrentGroup.Id)).ToArray();
            var xScale = selectedUnitsForScale.Sum(x => x.X) / selectedUnitsForScale.Length;
            var yScale = selectedUnitsForScale.Sum(x => x.Y) / selectedUnitsForScale.Length;

            ActionHelper.Scale(xScale, yScale, 0.1);

            RewindClient.RewindClient.Instance.Circle(xScale, yScale, 10, Color.Black);
        }

        private static void SelectUnitsOfType(VehicleType vehicleType)
        {
            var fighters = UnitHelper.Units.Select(x => x.Value).Where(x => x.Side == Side.Our)
                .Where(x => x.Type == vehicleType).ToArray();

            var minX = fighters.Min(x => x.X);
            var minY = fighters.Min(x => x.Y);
            var maxX = fighters.Max(x => x.X);
            var maxY = fighters.Max(x => x.Y);

            ActionHelper.Select(minX, minY, maxX, maxY);
        }
    }
}