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

    public class AddSelecteUnitsToNewGroupTask : QueueTask
    {
        public readonly VehicleType VehicleType;

        public AddSelecteUnitsToNewGroupTask(VehicleType vehicleType)
        {
            VehicleType = vehicleType;
        }
    }

    public class StartProduction : QueueTask
    {
        public readonly FacilityEx Facility;

        public StartProduction(FacilityEx facility)
        {
            Facility = facility;
        }
    }

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
