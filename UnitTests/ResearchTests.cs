using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class ResearchTests
    {
        [TestCase()]
        public void Test()
        {
            var source = new float[5, 5];

           var m = PotentialFieldsHelper.CreatePfEx(5);

            PotentialFieldsHelper.ApplyPower(source, 3, 3, m, 100);
        }
    }
}
