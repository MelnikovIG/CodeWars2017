using System;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public static class GeometryHelper
    {
        public static bool PointIsWithinCircle(double circleCenterPointX, double circleCenterPointY, double circleRadius, double pointToCheckX, double pointToCheckY)
        {
            return (Math.Pow(pointToCheckX - circleCenterPointX, 2) + Math.Pow(pointToCheckY - circleCenterPointY, 2)) < (Math.Pow(circleRadius, 2));
        }

        public static double GetDistancePower2To(double x1, double y1, double x2, double y2)
        {
            double xRange = x2 - x1;
            double yRange = y2 - y1;
            return xRange * xRange + yRange * yRange;
        }
    }
}
