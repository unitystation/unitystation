using System;
using System.Collections.Generic;


namespace Light2D.Examples
{
    /// <summary>
    /// SimpleRNG is a simple random number generator based on 
    /// George Marsaglia's MWC (multiply with carry) generator.
    /// Although it is very simple, it passes Marsaglia's DIEHARD
    /// series of random number generator tests.
    /// 
    /// Written by John D. Cook 
    /// http://www.johndcook.com
    /// </summary>
    public class SimpleRNG
    {
        public uint w;
        public uint z;

        public ulong longSeed
        {
            get { return ((ulong) w << 32) + (ulong) z; }
            set
            {
                w = (uint) (value >> 32);
                z = unchecked ((uint) value);
            }
        }

        public int seed
        {
            set { w = unchecked ((uint) value); }
        }

        public float value
        {
            get { return GetUniformF(); }
        }

        public SimpleRNG()
        {
            // These values are not magical, just the default values Marsaglia used.
            // Any pair of unsigned integers should be fine.
            w = 521288629;
            z = 362436069;
        }

        // The random generator seed can be set three ways:
        // 1) specifying two non-zero unsigned integers
        // 2) specifying one non-zero unsigned integer and taking a default value for the second
        // 3) setting the seed from the system time

        public SimpleRNG(uint u, uint v) : this()
        {
            if (u != 0) w = u;
            if (v != 0) z = v;
        }

        public SimpleRNG(uint u) : this()
        {
            w = u;
        }

        public SimpleRNG(int u) : this((uint) u)
        {
        }

        public static SimpleRNG FromSystemTime()
        {
            System.DateTime dt = System.DateTime.Now;
            long x = dt.ToFileTime();
            return new SimpleRNG((uint) (x >> 16), (uint) (x%4294967296));
        }

        // Produce a uniform random sample from the open interval (0, 1).
        // The method will not return either end point.
        public double GetUniform()
        {
            // 0 <= u < 2^32
            uint u = GetUint();
            // The magic number below is 1/(2^32 + 2).
            // The result is strictly between 0 and 1.
            return (u + 1.0)*2.328306435454494e-10;
        }

        public float GetUniformF()
        {
            return (float) GetUniform();
        }

        public int Range(int minValue, int maxValue)
        {
            return Range(maxValue - minValue) + minValue;
        }

        public int Range(int maxValue)
        {
            var num = (int) GetUint();
            if (num < 0) num = -num;
            return num%maxValue;
        }

        // This is the heart of the generator.
        // It uses George Marsaglia's MWC algorithm to produce an unsigned integer.
        // See http://www.bobwheeler.com/statistics/Password/MarsagliaPost.txt
        private uint GetUint()
        {
            z = 36969*(z & 65535) + (z >> 16);
            w = 18000*(w & 65535) + (w >> 16);
            return (z << 16) + w;
        }

        // Get normal (Gaussian) random sample with mean 0 and standard deviation 1
        public double GetNormal()
        {
            // Use Box-Muller algorithm
            double u1 = GetUniform();
            double u2 = GetUniform();
            double r = Math.Sqrt(-2.0*Math.Log(u1));
            double theta = 2.0*Math.PI*u2;
            return r*Math.Sin(theta);
        }

        // Get normal (Gaussian) random sample with specified mean and standard deviation
        public double GetNormal(double mean, double standardDeviation)
        {
            if (standardDeviation <= 0.0)
            {
                string msg = string.Format("Shape must be positive. Received {0}.", standardDeviation);
                throw new ArgumentOutOfRangeException(msg);
            }
            return mean + standardDeviation*GetNormal();
        }

        // Get exponential random sample with mean 1
        public double GetExponential()
        {
            return -Math.Log(GetUniform());
        }

        // Get exponential random sample with specified mean
        public double GetExponential(double mean)
        {
            if (mean <= 0.0)
            {
                string msg = string.Format("Mean must be positive. Received {0}.", mean);
                throw new ArgumentOutOfRangeException(msg);
            }
            return mean*GetExponential();
        }

        public double GetGamma(double shape, double scale)
        {
            // Implementation based on "A Simple Method for Generating Gamma Variables"
            // by George Marsaglia and Wai Wan Tsang.  ACM Transactions on Mathematical Software
            // Vol 26, No 3, September 2000, pages 363-372.

            double d, c, x, xsquared, v, u;

            if (shape >= 1.0)
            {
                d = shape - 1.0/3.0;
                c = 1.0/Math.Sqrt(9.0*d);
                for (;;)
                {
                    do
                    {
                        x = GetNormal();
                        v = 1.0 + c*x;
                    } while (v <= 0.0);
                    v = v*v*v;
                    u = GetUniform();
                    xsquared = x*x;
                    if (u < 1.0 - .0331*xsquared*xsquared || Math.Log(u) < 0.5*xsquared + d*(1.0 - v + Math.Log(v)))
                        return scale*d*v;
                }
            }
            else if (shape <= 0.0)
            {
                string msg = string.Format("Shape must be positive. Received {0}.", shape);
                throw new ArgumentOutOfRangeException(msg);
            }
            else
            {
                double g = GetGamma(shape + 1.0, 1.0);
                double w = GetUniform();
                return scale*g*Math.Pow(w, 1.0/shape);
            }
        }

        public double GetChiSquare(double degreesOfFreedom)
        {
            // A chi squared distribution with n degrees of freedom
            // is a gamma distribution with shape n/2 and scale 2.
            return GetGamma(0.5*degreesOfFreedom, 2.0);
        }

        public double GetInverseGamma(double shape, double scale)
        {
            // If X is gamma(shape, scale) then
            // 1/Y is inverse gamma(shape, 1/scale)
            return 1.0/GetGamma(shape, 1.0/scale);
        }

        public double GetWeibull(double shape, double scale)
        {
            if (shape <= 0.0 || scale <= 0.0)
            {
                string msg =
                    string.Format("Shape and scale parameters must be positive. Recieved shape {0} and scale{1}.",
                        shape, scale);
                throw new ArgumentOutOfRangeException(msg);
            }
            return scale*Math.Pow(-Math.Log(GetUniform()), 1.0/shape);
        }

        public double GetCauchy(double median, double scale)
        {
            if (scale <= 0)
            {
                string msg = string.Format("Scale must be positive. Received {0}.", scale);
                throw new ArgumentException(msg);
            }

            double p = GetUniform();

            // Apply inverse of the Cauchy distribution function to a uniform
            return median + scale*Math.Tan(Math.PI*(p - 0.5));
        }

        public double GetStudentT(double degreesOfFreedom)
        {
            if (degreesOfFreedom <= 0)
            {
                string msg = string.Format("Degrees of freedom must be positive. Received {0}.", degreesOfFreedom);
                throw new ArgumentException(msg);
            }

            // See Seminumerical Algorithms by Knuth
            double y1 = GetNormal();
            double y2 = GetChiSquare(degreesOfFreedom);
            return y1/Math.Sqrt(y2/degreesOfFreedom);
        }

        // The Laplace distribution is also known as the double exponential distribution.
        public double GetLaplace(double mean, double scale)
        {
            double u = GetUniform();
            return (u < 0.5)
                ? mean + scale*Math.Log(2.0*u)
                : mean - scale*Math.Log(2*(1 - u));
        }

        public double GetLogNormal(double mu, double sigma)
        {
            return Math.Exp(GetNormal(mu, sigma));
        }

        public double GetBeta(double a, double b)
        {
            if (a <= 0.0 || b <= 0.0)
            {
                string msg = string.Format("Beta parameters must be positive. Received {0} and {1}.", a, b);
                throw new ArgumentOutOfRangeException(msg);
            }

            // There are more efficient methods for generating beta samples.
            // However such methods are a little more efficient and much more complicated.
            // For an explanation of why the following method works, see
            // http://www.johndcook.com/distribution_chart.html#gamma_beta

            double u = GetGamma(a, 1.0);
            double v = GetGamma(b, 1.0);
            return u/(u + v);
        }
    }

    public static class SimpleRNGUtils
    {
        public static T RandomElement<T>(this IEnumerable<T> enumerable, Func<T, int> weightFunc, SimpleRNG rand)
        {
            int totalWeight = 0; // this stores sum of weights of all elements before current
            T selected = default(T); // currently selected element
            foreach (var data in enumerable)
            {
                int weight = weightFunc(data); // weight of current element
                int r = rand.Range(0, totalWeight + weight); // random value
                if (r >= totalWeight) // probability of this is weight/(totalWeight+weight)
                    selected = data;
                // it is the probability of discarding last selected element and selecting current one instead
                totalWeight += weight; // increase weight sum
            }

            return selected; // when iterations end, selected is some element of sequence. 
        }
    }
}