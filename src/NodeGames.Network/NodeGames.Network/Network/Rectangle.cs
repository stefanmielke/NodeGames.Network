namespace NodeGames.Network.Network
{
    public struct Rectangle
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public bool Intersects(Rectangle value)
        {
            return value.X < (X + Width) &&
                   X < (value.X + value.Width) &&
                   value.Y < (Y + Height) &&
                   Y < (value.Y + value.Height);
        }
    }
}