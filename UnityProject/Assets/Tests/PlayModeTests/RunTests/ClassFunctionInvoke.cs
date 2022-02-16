using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


[System.Serializable]
public class ClassFunctionInvoke
{

	public string VariablePath;


	private BindingFlags SetBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic |
	                                       BindingFlags.Static |
	                                       BindingFlags.FlattenHierarchy;

	public void Invoke(Type InType, object ClassObject)
	{
		var Path = VariablePath.Split('.');
		Type TMPType = InType;
		MemberInfo MI = null;
		object InstanceObject = ClassObject;

		MethodInfo MethodInfo = null;

		for (int i = 0; i < Path.Length; i++)
		{
			var Step = Path[i];
			if (MI != null)
			{
				InstanceObject = MI.GetValue(InstanceObject);
			}

			if ((i + 1) == Path.Length)
			{
				MethodInfo = TMPType.GetMethod(Step, SetBindingFlags);
				if (MethodInfo == null)
				{
					MethodInfo = TMPType?.BaseType?.GetMethod(Step, SetBindingFlags);
				}

				break;
			}


			MI = TMPType.GetField(Step, SetBindingFlags);
			if (MI == null)
			{
				MI = TMPType.GetProperty(Step, SetBindingFlags);
			}

			if (MI == null)
			{
				MI = TMPType?.BaseType?.GetField(Step, SetBindingFlags);
			}

			if (MI == null)
			{
				MI = TMPType?.BaseType?.GetProperty(Step, SetBindingFlags);
			}

			TMPType = MI.GetUnderlyingType();
		}

		MethodInfo.Invoke(InstanceObject, null);
	}
}
