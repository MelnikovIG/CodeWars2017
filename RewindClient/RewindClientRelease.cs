using System.Drawing;

#if !DEBUG
namespace RewindClient
{
    public class RewindClient
    {
        public static RewindClient Instance => new RewindClient();

        public void Rectangle(double centerX, double centerY, double maxX, double maxY, Color fromArgb)
        {
        }

        public void End()
        {
        }

        public void Circle(double unitX, double unitY, double unitVisionRange, Color fromArgb)
        {
        }
    }

    public enum UnitType
    {
        Unknown = 0,
        Tank = 1,
        Ifv = 2,
        Arrv = 3,
        Helicopter = 4,
        Fighter = 5,
    }

    public enum AreaType
    {
        Unknown = 0,
        Forest = 1,
        Swamp = 2,
        Rain = 3,
        Cloud = 4
    }
}
#endif