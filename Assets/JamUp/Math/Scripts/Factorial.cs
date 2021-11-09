namespace JamUp.Math
{
    public static class Factorial
    {
        public static long FactorialDivision(int topFactorial, int divisorFactorial)
        {
            long result = 1;
            for (int i = topFactorial; i > divisorFactorial; i--)
            {
                result *= i;
            }
            return result;
        }

        public static long Get(int i)
        {
            if (i <= 1)
            {
                return 1;
            }
            return i * Get(i - 1);
        }
    }
}