using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class QueueHelper
    {
        public static Queue<QueueTask> Queue { get; set; } = new Queue<QueueTask>();
    }

    public abstract class QueueTask
    {
        public abstract void Execute();
    }

    /// <summary>
    /// Скейл выбранных юнитов
    /// </summary>
    public class Scale : QueueTask
    {
        public double X { get; }
        public double Y { get; }
        public double Factor { get; }

        public Scale(double x, double y, double factor)
        {
            X = x;
            Y = y;
            Factor = factor;
        }

        public override void Execute()
        {
            ActionHelper.Scale(X, Y, Factor);
        }
    }

    /// <summary>
    /// Выбор юнитов
    /// </summary>
    public class SelectUnits : QueueTask
    {
        public double Left { get; }
        public double Top { get; }
        public double Right { get; }
        public double Bottom { get; }
        public VehicleType? VehicleType { get; }

        public SelectUnits(double left, double top, double right, double bottom, VehicleType? vehicleType = null)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
            VehicleType = vehicleType;
        }

        public override void Execute()
        {
            ActionHelper.Select(Left, Top, Right, Bottom, VehicleType);
        }
    }

    /// <summary>
    /// Выбор группы
    /// </summary>
    public class SelectGroup : QueueTask
    {
        public Group Group { get; }

        public SelectGroup(Group @group)
        {
            Group = @group;
        }

        public override void Execute()
        {
            ActionHelper.SelectGroup(Group);
        }
    }

    /// <summary>
    /// Добавление выбранных юнитов в группу
    /// </summary>
    public class AddSelecteUnitsToNewGroupTask : QueueTask
    {
        public readonly VehicleType VehicleType;

        public AddSelecteUnitsToNewGroupTask(VehicleType vehicleType)
        {
            VehicleType = vehicleType;
        }

        public override void Execute()
        {
            GroupHelper.CreateFroupForSelected(VehicleType);
        }
    }

    /// <summary>
    /// Сжать выбранную группу к центру
    /// </summary>
    public class ScaleCurrentGroupToCenterTask : QueueTask
    {
        public override void Execute()
        {
            var selectedUnitsForScale = UnitHelper.UnitsAlly.Where(x => x.Groups.Contains(GroupHelper.CurrentGroup.Id)).ToArray();
            var xScale = selectedUnitsForScale.Sum(x => x.X) / selectedUnitsForScale.Length;
            var yScale = selectedUnitsForScale.Sum(x => x.Y) / selectedUnitsForScale.Length;

            ActionHelper.Scale(xScale, yScale, 0.1);

            RewindClient.RewindClient.Instance.Circle(xScale, yScale, 10, Color.Black);
        }
    }

    /// <summary>
    /// Начало производства на заводе
    /// </summary>
    public class StartProduction : QueueTask
    {
        public readonly FacilityEx Facility;

        public StartProduction(FacilityEx facility)
        {
            Facility = facility;
        }

        public override void Execute()
        {
            FacilityProductionHelper.StartFactoryProduction(Facility, MyStrategy.LazyClusters.Value);
        }
    }

    /// <summary>
    /// Нанесение ядерного удара противнику
    /// </summary>
    public class NuclearStrike : QueueTask
    {
        public long VehicleId { get; }
        public double X { get; }
        public double Y { get; }

        public NuclearStrike(long vehicleId, double x, double y)
        {
            VehicleId = vehicleId;
            X = x;
            Y = y;
        }

        public override void Execute()
        {
            ActionHelper.NuclearStrike(VehicleId, X, Y);
        }
    }
}
