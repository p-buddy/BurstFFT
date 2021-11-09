namespace JamUp.Math
{
    public static class PowersOf2
    {
        public static int Next(int x)
        {
            if (x < 0)
            {
                return 0;
            }
            if (x == 0)
            {
                return 1;
            }
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }
        
        public static bool IsPowerOf2(int x)
        {
            return x != 0 && (x & (x - 1)) == 0;
        }
    }
}