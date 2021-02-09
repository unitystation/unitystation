using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allow you edit temerature float in different units in inspector
/// Float should be in Kelvin
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class TemperatureAttribute : PropertyAttribute
{
}