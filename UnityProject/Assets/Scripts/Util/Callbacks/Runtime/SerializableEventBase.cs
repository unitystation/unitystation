using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class SerializableEventBase : SerializableCallbackBase {
	public InvokableEventBase invokable;

	public override void ClearCache() {
		base.ClearCache();
		invokable = null;
	}

	protected InvokableEventBase GetPersistentMethod() {
		Type[] types = new Type[ArgTypes.Length];
		Array.Copy(ArgTypes, types, ArgTypes.Length);

		Type genericType = null;
		switch (types.Length) {
			case 0:
				genericType = typeof(InvokableEvent);
				break;
			case 1:
				genericType = typeof(InvokableEvent<>).MakeGenericType(types);
				break;
			case 2:
				genericType = typeof(InvokableEvent<,>).MakeGenericType(types);
				break;
			case 3:
				genericType = typeof(InvokableEvent<, ,>).MakeGenericType(types);
				break;
			case 4:
				genericType = typeof(InvokableEvent<, , ,>).MakeGenericType(types);
				break;
			default:
				throw new ArgumentException(types.Length + "args");
		}
		return Activator.CreateInstance(genericType, new object[] { target, methodName }) as InvokableEventBase;
	}
}