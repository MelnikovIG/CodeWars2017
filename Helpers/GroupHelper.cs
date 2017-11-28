using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class GroupHelper
    {
        private static int NextGroupId { get; set; } = 1;
        public static List<Group> Groups { get; set; } = new List<Group>();
        public static Group CurrentGroup { get; set; }

        public static Group CreateFroupForSelected(VehicleType vehicleType)
        {
            var newGroup = new Group(NextGroupId, vehicleType);
            Groups.Add(newGroup);

            ActionHelper.SetSelectedGroup(newGroup);

            NextGroupId++;

            return newGroup;
        }

        public static bool SelectNextGroup()
        {
            var currentGroup = GroupHelper.CurrentGroup;
            var currentGroupIndex = Groups.IndexOf(currentGroup);

            if (currentGroupIndex < 0)
            {
                throw new NotImplementedException("currentGroupIndex < 0");
            }

            Group nextSelectedGroup;

            do
            {
                var nextGroupIdx = currentGroupIndex == Groups.Count - 1 ? 0 : currentGroupIndex + 1;
                nextSelectedGroup = Groups[nextGroupIdx];

                var newGroupUnitsCount = UnitHelper.UnitsAlly
                    .Where(x => x.Groups.Contains(nextSelectedGroup.Id)).ToArray();

                if (newGroupUnitsCount.Length > 0)
                {
                    ActionHelper.SelectGroup(nextSelectedGroup);
                    return true;
                }
                currentGroupIndex = nextGroupIdx;

            } while (nextSelectedGroup != currentGroup);

            return false;
        }
    }

    public class Group
    {
        public int Id { get; set; }
        public VehicleType VehicleType { get; set; }

        public Group(int id, VehicleType vehicleType)
        {
            Id = id;
            VehicleType = vehicleType;
        }
    }
}
