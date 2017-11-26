using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class FacilityHelper
    {
        public static Dictionary<long, FacilityEx> Facilities =  new Dictionary<long, FacilityEx>();
    }

    public class FacilityEx
    {
        //public long Id { get; set; }
        //public long OwnerPlayerId { get; set; }
        //public int ProductionProgress { get; set; }
        public Side Side { get; set; }
        public FacilityType Type { get; set; }
        public double CapturePoints { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
    }
}
