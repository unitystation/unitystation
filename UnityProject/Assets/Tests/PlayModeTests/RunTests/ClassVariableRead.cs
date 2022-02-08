using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NaughtyAttributes;
using UnityEngine;

[System.Serializable]
public class ClassVariableRead
{
	public string VariablePath;
	//Classes are defined so
	//location.x
	//Have a list of these ClassVariableRead


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

	[AllowNesting] [ShowIf(nameof(ShowNumber))] public bool HasMin = false;
	[AllowNesting] [EnableIf("HasMin")] [ShowIf(nameof(ShowNumber))] public float Min;

	[AllowNesting] [ShowIf(nameof(ShowNumber))] public bool HasMax = false;
	[AllowNesting] [EnableIf("HasMax")] [ShowIf(nameof(ShowNumber))] public float Max;


	private BindingFlags SetBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic |
	                                       BindingFlags.Static |
	                                       BindingFlags.FlattenHierarchy;


	public bool SatisfiesConditions(Type InType, object ClassObject, out string FailReport)
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


		switch (VariableType)
		{
			case VariableCheckType.Bool:
				var Potentialbool = (MI.GetValue(InstanceObject) as bool?);
				if (Potentialbool == null)
				{
					FailReport = $"at {VariablePath} Variable did not match expected Type ( is in you specified a Number when it actually was a bool or string) The type of variable is {MI.GetUnderlyingType()} Was expecting bool ";
					return false;
				}

				if (Potentialbool.Value != TargetBoolValue)
				{
					FailReport = $"at {VariablePath} Variable bool did not match Expected {TargetBoolValue} was {Potentialbool.Value} ";
					return false;
				}
				else
				{
					FailReport = "";
					return true;
				}

			case VariableCheckType.String:
				string Value = MI.GetValue(InstanceObject) as string;
				if (Value != TargetStringValue)
				{
					FailReport = $"at {VariablePath} Variable String did not match Expected {TargetStringValue} was {Value} ";
					return false;
				}
				else
				{
					FailReport = "";
					return true;
				}
			case VariableCheckType.Number:
				var PotentialNumberValue = (MI.GetValue(InstanceObject) as float?);
				if (PotentialNumberValue == null)
				{
					FailReport = $"at {VariablePath} Variable did not match expected Type ( is in you specified a Number when it actually was a bool or string) The type of variable is {MI.GetUnderlyingType()} Was expecting Float/any type of number Type ";
					return false;
				}

				if (HasMin || HasMax)
				{
					if (HasMin)
					{
						if (Min > PotentialNumberValue.Value)
						{
							FailReport = $"at {VariablePath} Value was below minimum of {Min} at {PotentialNumberValue.Value} ";
							return false;
						}
					}

					if (HasMax)
					{
						if (Max < PotentialNumberValue.Value)
						{
							FailReport = $"at {VariablePath} Value was Over maximum of {Max} at {PotentialNumberValue.Value} ";
							return false;
						}
					}

					FailReport = "";
					return true;
				}
				else
				{
					if (PotentialNumberValue.Value != TargetValue)
					{
						FailReport = $"at {VariablePath} Value did not match The TargetValue of {TargetValue} was {PotentialNumberValue.Value}";
						return false;
					}
					else
					{
						FailReport = "";
						return true;
					}
				}
		}

		FailReport = "Test was not within switch statement, impossible HELP!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!";
		return false;
	}
}