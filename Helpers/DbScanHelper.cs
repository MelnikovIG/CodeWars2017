using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Helpers
{
    public class DbScanHelper
    {
        public class Point
        {
            public const int NOISE = -1;
            public const int UNCLASSIFIED = 0;
            public double X;
            public double Y;
            public VehicleType Type { get; }
            public int Durability { get; }
            public int ClusterId;

            public Point(double x, double y, VehicleType vehicleType, int durability)
            {
                this.X = x;
                this.Y = y;
                Type = vehicleType;
                Durability = durability;
            }

            public static double DistanceSquared(Point p1, Point p2)
            {
                var diffX = p2.X - p1.X;
                var diffY = p2.Y - p1.Y;
                return diffX * diffX + diffY * diffY;
            }
        }

        public static List<List<Point>> GetClusters(List<Point> points, double eps, int minPts)
        {
            if (points == null || points.Count == 0) return new List<List<Point>>();
            List<List<Point>> clusters = new List<List<Point>>();
            eps *= eps; // square eps
            int clusterId = 1;
            for (int i = 0; i < points.Count; i++)
            {
                Point p = points[i];
                if (p.ClusterId == Point.UNCLASSIFIED)
                {
                    if (ExpandCluster(points, p, clusterId, eps, minPts)) clusterId++;
                }
            }
            // sort out points into their clusters, if any
            int maxClusterId = points.OrderBy(p => p.ClusterId).Last().ClusterId;
            if (maxClusterId < 1) return clusters; // no clusters, so list is empty
            for (int i = 0; i < maxClusterId; i++) clusters.Add(new List<Point>());
            foreach (Point p in points)
            {
                if (p.ClusterId > 0) clusters[p.ClusterId - 1].Add(p);
            }
            return clusters;
        }

        static List<Point> GetRegion(List<Point> points, Point p, double eps)
        {
            List<Point> region = new List<Point>();
            for (int i = 0; i < points.Count; i++)
            {
                var distSquared = Point.DistanceSquared(p, points[i]);
                if (distSquared <= eps) region.Add(points[i]);
            }
            return region;
        }

        static bool ExpandCluster(List<Point> points, Point p, int clusterId, double eps, int minPts)
        {
            List<Point> seeds = GetRegion(points, p, eps);
            if (seeds.Count < minPts) // no core point
            {
                p.ClusterId = Point.NOISE;
                return false;
            }
            else // all points in seeds are density reachable from point 'p'
            {
                for (int i = 0; i < seeds.Count; i++) seeds[i].ClusterId = clusterId;
                seeds.Remove(p);
                while (seeds.Count > 0)
                {
                    Point currentP = seeds[0];
                    List<Point> result = GetRegion(points, currentP, eps);
                    if (result.Count >= minPts)
                    {
                        for (int i = 0; i < result.Count; i++)
                        {
                            Point resultP = result[i];
                            if (resultP.ClusterId == Point.UNCLASSIFIED || resultP.ClusterId == Point.NOISE)
                            {
                                if (resultP.ClusterId == Point.UNCLASSIFIED) seeds.Add(resultP);
                                resultP.ClusterId = clusterId;
                            }
                        }
                    }
                    seeds.Remove(currentP);
                }
                return true;
            }
        }

        public static void DrawClusters(List<List<Point>> clusters)
        {
            //foreach (var cluster in clusters)
            //{
            //    var minX = cluster.Min(x => x.X);
            //    var minY = cluster.Min(x => x.Y);
            //    var maxX = cluster.Max(x => x.X);
            //    var maxY = cluster.Max(x => x.Y);

            //    RewindClient.RewindClient.Instance.Rectangle(minX - 2, minY - 2, maxX + 2, maxY + 2,
            //        Color.FromArgb(100, 0, 255, 255));
            //}
        }
    }
}
