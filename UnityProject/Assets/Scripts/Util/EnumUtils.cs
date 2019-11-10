using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

public static class EnumUtils
{
	public static string GetDescription(this Enum value)
	{
		FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
		if (fieldInfo == null)
		{
			return null;
		}
		DescriptionAttribute descriptionAttribute =
			fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
		return descriptionAttribute == null ? value.ToString() : descriptionAttribute.Description;
	}
	public static int GetOrder(this Enum value)
	{
		FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
		OrderAttribute attribute =
			fieldInfo.GetCustomAttributes(typeof(OrderAttribute), false).FirstOrDefault() as OrderAttribute;
		return attribute == null ? -1 : attribute.Order;
	}

	public static int GetSetBitCount(long lValue)
	{
		int iCount = 0;

		while (lValue != 0)
		{
			lValue = lValue & (lValue - 1);
			iCount++;
		}

		return iCount;
	}
}