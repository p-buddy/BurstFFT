namespace JamUp.Math
{
    public static class Combination
    {
        public static long nCr(int n, int r)
        {
            // naive: return Factorial(n) / (Factorial(r) * Factorial(n - r));
            return Permutation.nPr(n, r) / Factorial.Get(r);
        }
    }
}