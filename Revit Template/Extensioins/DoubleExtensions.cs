using System;

namespace RevitTemplate.Extensioins
{
    public static class DoubleExtensions
    {
        public const double FootToMFactor = 0.3048;

        public static double SqFootToSqM(this double area, int round = 2)
        {
            return Math.Round(
                FootToMFactor * FootToMFactor * area, round);
        }
    }
}
