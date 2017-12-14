using System;
using System.ComponentModel;
using System.Linq;

public static class EnumUtils
{
    public static string GetDescription(this Enum value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString());
        if (fieldInfo == null)
        {
            return null;
        }
        var descriptionAttribute =
            fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
        return descriptionAttribute == null ? value.ToString() : descriptionAttribute.Description;
    }

    public static int GetSetBitCount(long lValue)
    {
        var iCount = 0;

        while (lValue != 0)
        {
            lValue = lValue & (lValue - 1);
            iCount++;
        }

        return iCount;
    }
}