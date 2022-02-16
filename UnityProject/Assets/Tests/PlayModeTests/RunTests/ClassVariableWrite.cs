using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NaughtyAttributes;
using UnityEngine;

[System.Serializable]
public class ClassVariableWriter
{
	public string VariablePath;

	public VariableCheckType VariableType;

	public enum VariableCheckType
	{
		String,
		Number,
		Bool
	}

	public bool ShowBool => VariableType == VariableCheckType.Bool;
	[AllowNesting] [ShowIf(nameof(ShowBool))] public bool TargetBoolValue;

	public bool ShowString => VariableType == VariableCheckType.String;
	[AllowNesting] [ShowIf(nameof(ShowString))] public string TargetStringValue;

	public bool ShowNumber => VariableType == VariableCheckType.Number;
	[AllowNesting] [ShowIf(nameof(ShowNumber))] public float TargetValue;



	private const BindingFlags SetBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic |
	                                       BindingFlags.Static |
	                                       BindingFlags.FlattenHierarchy;


	public void SetValue(Type InType, object ClassObject)
	{
		var Path = VariablePath.Split('.');
		Type TMPType = InType;
		MemberInfo MI = null;
		object InstanceObject = ClassObject;

		foreach (var Step in Path)
		{
			if (MI != null)
			{
				InstanceObject = MI.GetValue(InstanceObject);
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

		object ValueToSet = null;

		switch (VariableType)
		{
			case VariableCheckType.Bool:
				ValueToSet = TargetBoolValue;
				break;
			case VariableCheckType.Number:
				ValueToSet = TargetValue;
				break;
			case VariableCheckType.String:
				ValueToSet = TargetStringValue;
				break;
		}

		MI.MemberInfoSetValue(InstanceObject, ValueToSet);

	}




}
