using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class DMMath
{
    public static double Round(double n, double x)
    {
        return Math.Round(n / x) * x;
    }

    public static T Max<T>(T[] itemArr)
    {
        return itemArr.Max();
    }

    public static int Clamp(int val, int min, int max)
    {
        return Mathf.Clamp(val, min, max);
    }

    public static bool Prob(double percent)
    {
        System.Random rand = new System.Random(Guid.NewGuid().GetHashCode());
        return rand.NextDouble() < percent / 100.0;
    }
}
