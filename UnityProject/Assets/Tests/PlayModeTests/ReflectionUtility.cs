using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class ReflectionUtility
{
	public static Type GetUnderlyingType(this MemberInfo member)
	{
		switch (member.MemberType)
		{
			case MemberTypes.Event:
				return ((EventInfo)member).EventHandlerType;
			case MemberTypes.Field:
				return ((FieldInfo)member).FieldType;
			case MemberTypes.Method:
				return ((MethodInfo)member).ReturnType;
			case MemberTypes.Property:
				return ((PropertyInfo)member).PropertyType;
			default:
				throw new ArgumentException
				(
					"Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo"
				);
		}
	}

	public static object GetValue(this MemberInfo memberInfo, object forObject)
	{
		switch (memberInfo.MemberType)
		{
			case MemberTypes.Field:
				return ((FieldInfo)memberInfo).GetValue(forObject);
			case MemberTypes.Property:
				return ((PropertyInfo)memberInfo).GetValue(forObject);
			default:
				throw new NotImplementedException();
		}
	}

	public static void MemberInfoSetValue(this MemberInfo memberInfo, object ClassObject, object NewVariableObject )
	{
		switch (memberInfo.MemberType)
		{
			case MemberTypes.Field:
				((FieldInfo)memberInfo).SetValue(ClassObject,NewVariableObject);
				break;
			case MemberTypes.Property:
				((PropertyInfo)memberInfo).SetValue(ClassObject,NewVariableObject );
				break;
			default:
				throw new NotImplementedException();
		}
	}
}
