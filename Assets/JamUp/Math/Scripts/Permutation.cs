namespace JamUp.Math
{
    public static class Permutation
    {
        public static long nPr(int n, int r)
        {
            // naive: return Factorial(n) / Factorial(n - r);
            return Factorial.FactorialDivision(n, n - r);
        }
    }
}