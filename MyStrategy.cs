using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;
using Side = Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers.Side;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk
{
    public sealed class MyStrategy : IStrategy
    {
        private static string EndOfString = " ";

        public void Move(Player me, World world, Game game, Move move)
        {
            var rewindClient = RewindClient.RewindClient.Instance;

#if DEBUG
            rewindClient.Message($"Queue length before move: {QueueHelper.Queue.Count}" + EndOfString);
            foreach (var queueItem in QueueHelper.Queue)
            {
                rewindClient.Message(queueItem.GetType().Name + EndOfString);
            }
            rewindClient.Message("-----------------------------------" + EndOfString);
#endif
            try
            {
                MoveEx(me, world, game, move, rewindClient);
            }
            catch (Exception e)
            {
                //TODO: comment on final deploy
                throw GlobalHelper.GetException(e.Message);
            }

#if DEBUG
            rewindClient.Message("-----------------------------------" + EndOfString);
            rewindClient.Message($"Queue length after move: {QueueHelper.Queue.Count}" + EndOfString);
            foreach (var queueItem in QueueHelper.Queue)
            {
                rewindClient.Message(queueItem.GetType().Name + EndOfString);
            }
#endif

            rewindClient.End();
        }

        public static Lazy<List<List<DbScanHelper.Point>>> LazyClusters;

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

            LazyClusters = new Lazy<List<List<DbScanHelper.Point>>>(() =>
            {
                var enemyPoints = UnitHelper.UnitsEnemy.Select(x => new DbScanHelper.Point(x.X, x.Y, x.Type, x.Durability)).ToList();
                List<List<DbScanHelper.Point>> clusters = DbScanHelper.GetClusters(enemyPoints, 15, 1);
                return clusters;
            });

#if DEBUG
            UnitHelper.DrawAllUnits();
            DbScanHelper.DrawClusters(LazyClusters.Value);
            FacilityHelper.DrawFacilities();
            NuclearStrikeHelper.DrawNuclearStrikes(me, enemy, game, rewindClient);
#endif

            if (world.TickIndex == 0)
            {
                PrepareUnits();
            }

            if (QueueHelper.Queue.Count > 0)
            {
                if (!GlobalHelper.MoveAllowed)
                {
                    return;
                }

                var task = QueueHelper.Queue.Dequeue();
                task.Execute();
                return;
            }

            var nucStrikeProcessed = NuclearStrikeHelper.ProcessEnemyNuclearStrikeDodge(GlobalHelper.MoveAllowed);
            if (nucStrikeProcessed)
            {
                return;
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

            var applyNuclearStrikePower = me.RemainingNuclearStrikeCooldownTicks <= 0 &&
                                          ConfigurationHelper.EnableNuclearStrike;

            PotentialFieldsHelper.AppendEnemyPower(LazyClusters.Value, applyNuclearStrikePower);
            PotentialFieldsHelper.ApplyHealPower();

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
                    new ScaleCurrentGroupToCenterTask().Execute();
                }
                else
                {
                    var cx = selectedUnits.Sum(x => x.X) / selectedUnits.Length;
                    var cy = selectedUnits.Sum(x => x.Y) / selectedUnits.Length;

                    var nextPpPoint = PotentialFieldsHelper.Get_PP_PointToMove(cx, cy, 3);
                    var quartCellLength = PotentialFieldsHelper.PpSize * 0.25;

                    var nextPpPointX = nextPpPoint.X * size + size / 2d;
                    var nextPpPointY = nextPpPoint.Y * size + size / 2d;

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

                    var vx = nextPpPointX - cx;
                    var vy = nextPpPointY - cy;

#if  DEBUG
                    rewindClient.Line(cx, cy, nextPpPointX, nextPpPointY, Color.Black);
#endif

                    ActionHelper.Move(vx, vy);
                }

                currentSelectedGroup.Moved = true;
                currentSelectedGroup.MovesCount++;

                return;
            }
            return;
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
        }

        private static void PrepareUnits()
        {
            var mapWidth = GlobalHelper.World.Width;
            var mapHeight = GlobalHelper.World.Height;

            var queue = QueueHelper.Queue;

            queue.Enqueue(new SelectUnits(0, 0, mapWidth, mapHeight, VehicleType.Fighter));
            queue.Enqueue(new AddSelecteUnitsToNewGroupTask(VehicleType.Fighter));
            queue.Enqueue(new ScaleCurrentGroupToCenterTask());

            queue.Enqueue(new SelectUnits(0, 0, mapWidth, mapHeight, VehicleType.Helicopter));
            queue.Enqueue(new AddSelecteUnitsToNewGroupTask(VehicleType.Helicopter));
            queue.Enqueue(new ScaleCurrentGroupToCenterTask());

            queue.Enqueue(new SelectUnits(0, 0, mapWidth, mapHeight, VehicleType.Tank));
            queue.Enqueue(new AddSelecteUnitsToNewGroupTask(VehicleType.Tank));
            queue.Enqueue(new ScaleCurrentGroupToCenterTask());

            queue.Enqueue(new SelectUnits(0, 0, mapWidth, mapHeight, VehicleType.Ifv));
            queue.Enqueue(new AddSelecteUnitsToNewGroupTask(VehicleType.Ifv));
            queue.Enqueue(new ScaleCurrentGroupToCenterTask());

            queue.Enqueue(new SelectUnits(0, 0, mapWidth, mapHeight, VehicleType.Arrv));
            queue.Enqueue(new AddSelecteUnitsToNewGroupTask(VehicleType.Arrv));
            queue.Enqueue(new ScaleCurrentGroupToCenterTask());

            queue.Enqueue(new SelectUnits(0, 0, mapWidth, mapHeight, VehicleType.Fighter));
        }
    }
}