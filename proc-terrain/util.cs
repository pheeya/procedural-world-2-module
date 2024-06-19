using System;

namespace ProcWorld
{
    public class util
    {
        public static int LCGRandom(int seed, int minValue, int maxValue)
        {
            int a = 1664525;
            int c = 1013904223;



// simplified version by chatgpt probably using some poor man's stack overflow 
// it just multiplied the seed with some random numbers
// if you want proper LCG, refer to the wiki page. 
            // LCG: X_n+1 = (a * X_n + c) % m
            seed = a * seed + c;

            // Map the seed to a value in the desired range
            int range = maxValue - minValue;
            return (Math.Abs(seed) % range) + minValue;
        }
    }
}