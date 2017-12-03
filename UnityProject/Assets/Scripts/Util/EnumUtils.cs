using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;


    public static class EnumUtils
    {
        public static string GetDescription(this Enum value)
        {
            FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
            if (fieldInfo == null) return null;
            var descriptionAttribute = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
            return descriptionAttribute == null ? value.ToString() : descriptionAttribute.Description;
        }
    }