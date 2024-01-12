using UnityEngine;
using System;

/// <summary> Add to fields of your class extending SerializableCallbackBase<T,..> to limit which types can be assigned to it. </summary>
public class TargetConstraintAttribute : PropertyAttribute {
	public Type targetType;

	/// <summary> Add to fields of your class extending SerializableCallbackBase<T,..> to limit which types can be assigned to it. </summary>
	public TargetConstraintAttribute(Type targetType) {
		this.targetType = targetType;
	}
}
