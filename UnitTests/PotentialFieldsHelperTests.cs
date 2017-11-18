using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestFixture]
    public class PotentialFieldsHelperTests
    {
        [TestCase(992.98346867860641, 983.02623494780937)]
        public void GetNextSafest_PP_PointByWorldXY_ShouldNotThrowExceptions(double cx, double cy)
        {
            Assert.DoesNotThrow(() => {
                PotentialFieldsHelper.GetNextSafest_PP_PointByWorldXY(cx, cy);
            });
        }
    }
}
