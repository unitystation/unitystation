using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvokableCallback<TReturn> : InvokableCallbackBase<TReturn> {

	public Func<TReturn> func;

	public TReturn Invoke() {
		return func();
	}

	public override TReturn Invoke(params object[] args) {
		return func();
	}

	/// <summary> Constructor </summary>
	public InvokableCallback(object target, string methodName) {
		if (target == null || string.IsNullOrEmpty(methodName)) {
			func = () => default(TReturn);
		} else {
			func = (System.Func<TReturn>) System.Delegate.CreateDelegate(typeof(System.Func<TReturn>), target, methodName);
		}
	}
}

public class InvokableCallback<T0, TReturn> : InvokableCallbackBase<TReturn> {

	public Func<T0, TReturn> func;

	public TReturn Invoke(T0 arg0) {
		return func(arg0);
	}

	public override TReturn Invoke(params object[] args) {
		// Convert from special "unity-nulls" to true null
		if (args[0] is UnityEngine.Object && (UnityEngine.Object) args[0] == null) args[0] = null;
		return func((T0) args[0]);
	}

	/// <summary> Constructor </summary>
	public InvokableCallback(object target, string methodName) {
		if (target == null || string.IsNullOrEmpty(methodName)) {
			func = x => default(TReturn);
		} else {
			func = (System.Func<T0, TReturn>) System.Delegate.CreateDelegate(typeof(System.Func<T0, TReturn>), target, methodName);
		}
	}
}

public class InvokableCallback<T0, T1, TReturn> : InvokableCallbackBase<TReturn> {

	public Func<T0, T1, TReturn> func;

	public TReturn Invoke(T0 arg0, T1 arg1) {
		return func(arg0, arg1);
	}

	public override TReturn Invoke(params object[] args) {
		// Convert from special "unity-nulls" to true null
		if (args[0] is UnityEngine.Object && (UnityEngine.Object) args[0] == null) args[0] = null;
		if (args[1] is UnityEngine.Object && (UnityEngine.Object) args[1] == null) args[1] = null;
		return func((T0) args[0], (T1) args[1]);
	}

	/// <summary> Constructor </summary>
	public InvokableCallback(object target, string methodName) {
		if (target == null || string.IsNullOrEmpty(methodName)) {
			func = (x, y) => default(TReturn);
		} else {
			func = (System.Func<T0, T1, TReturn>) System.Delegate.CreateDelegate(typeof(System.Func<T0, T1, TReturn>), target, methodName);
		}
	}
}

public class InvokableCallback<T0, T1, T2, TReturn> : InvokableCallbackBase<TReturn> {

	public Func<T0, T1, T2, TReturn> func;

	public TReturn Invoke(T0 arg0, T1 arg1, T2 arg2) {
		return func(arg0, arg1, arg2);
	}

	public override TReturn Invoke(params object[] args) {
		// Convert from special "unity-nulls" to true null
		if (args[0] is UnityEngine.Object && (UnityEngine.Object) args[0] == null) args[0] = null;
		if (args[1] is UnityEngine.Object && (UnityEngine.Object) args[1] == null) args[1] = null;
		if (args[2] is UnityEngine.Object && (UnityEngine.Object) args[2] == null) args[2] = null;
		return func((T0) args[0], (T1) args[1], (T2) args[2]);
	}

	/// <summary> Constructor </summary>
	public InvokableCallback(object target, string methodName) {
		if (target == null || string.IsNullOrEmpty(methodName)) {
			func = (x, y, z) => default(TReturn);
		} else {
			func = (System.Func<T0, T1, T2, TReturn>) System.Delegate.CreateDelegate(typeof(System.Func<T0, T1, T2, TReturn>), target, methodName);
		}
	}
}

public class InvokableCallback<T0, T1, T2, T3, TReturn> : InvokableCallbackBase<TReturn> {

	public Func<T0, T1, T2, T3, TReturn> func;

	public TReturn Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3) {
		return func(arg0, arg1, arg2, arg3);
	}

	public override TReturn Invoke(params object[] args) {
		// Convert from special "unity-nulls" to true null
		if (args[0] is UnityEngine.Object && (UnityEngine.Object) args[0] == null) args[0] = null;
		if (args[1] is UnityEngine.Object && (UnityEngine.Object) args[1] == null) args[1] = null;
		if (args[2] is UnityEngine.Object && (UnityEngine.Object) args[2] == null) args[2] = null;
		if (args[3] is UnityEngine.Object && (UnityEngine.Object) args[3] == null) args[3] = null;
		return func((T0) args[0], (T1) args[1], (T2) args[2], (T3) args[3]);
	}

	/// <summary> Constructor </summary>
	public InvokableCallback(object target, string methodName) {
		if (target == null || string.IsNullOrEmpty(methodName)) {
			func = (x, y, z, w) => default(TReturn);
		} else {
			func = (System.Func<T0, T1, T2, T3, TReturn>) System.Delegate.CreateDelegate(typeof(System.Func<T0, T1, T2, T3, TReturn>), target, methodName);
		}
	}
}