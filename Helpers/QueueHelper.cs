using System;
using System.Collections.Generic;
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
    }
}
