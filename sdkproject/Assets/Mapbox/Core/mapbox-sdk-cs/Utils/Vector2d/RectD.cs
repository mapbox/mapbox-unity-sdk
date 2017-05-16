namespace Mapbox.Utils
{
    public class RectD
    {
        public Vector2d Min { get; private set; }
        public Vector2d Max { get; private set; }
        //size is absolute width&height so Min+size != max
        public Vector2d Size { get; private set; }
        public Vector2d Center { get; private set; }
        
        public RectD(Vector2d min, Vector2d size)
        {
            Min = min;
            Max = min + size;
            Center = new Vector2d(Min.x + size.x / 2, Min.y + size.y / 2);
            Size = new Vector2d(Mathd.Abs(size.x), Mathd.Abs(size.y));
        }

        public bool Contains(Vector2d point)
        {
            bool flag = Size.x < 0.0 && point.x <= Min.x && point.x > (Min.x + Size.x) || Size.x >= 0.0 && point.x >= Min.x && point.x < (Min.x + Size.x);
            return flag && (Size.y < 0.0 && point.y <= Min.y && point.y > (Min.y + Size.y) || Size.y >= 0.0 && point.y >= Min.y && point.y < (Min.y + Size.y));
        }
    }
}