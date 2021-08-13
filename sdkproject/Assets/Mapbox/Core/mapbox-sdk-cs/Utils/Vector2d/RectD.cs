namespace Mapbox.Utils
{
    public struct RectD
    {
        public Vector2d TopLeft { get; private set; }
        public Vector2d BottomRight { get; private set; }
        //size is absolute width&height so Min+size != max
        public Vector2d Size { get; private set; }
        public Vector2d Center { get; private set; }
        
        public RectD(Vector2d topLeft, Vector2d size)
        {
            TopLeft = topLeft;
            BottomRight = topLeft + size;
            Center = new Vector2d(TopLeft.x + size.x / 2, TopLeft.y + size.y / 2);
            Size = new Vector2d(Mathd.Abs(size.x), Mathd.Abs(size.y));
        }

        public bool Contains(Vector2d point)
        {
            bool flag = Size.x < 0.0 && point.x <= TopLeft.x && point.x > (TopLeft.x + Size.x) || Size.x >= 0.0 && point.x >= TopLeft.x && point.x < (TopLeft.x + Size.x);
            return flag && (Size.y < 0.0 && point.y <= TopLeft.y && point.y > (TopLeft.y + Size.y) || Size.y >= 0.0 && point.y >= TopLeft.y && point.y < (TopLeft.y + Size.y));
        }
    }
}