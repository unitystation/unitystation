using System;

public class InvokableEvent : InvokableEventBase {

	public System.Action action;

	public void Invoke() {
		action();
	}

	public override void Invoke(params object[] args) {
		action();
	}

	/// <summary> Constructor </summary>
	public InvokableEvent(object target, string methodName) {
		if (target == null || string.IsNullOrEmpty(methodName)) {
			action = () => { };
		} else {
			action = (System.Action) System.Delegate.CreateDelegate(typeof(System.Action), target, methodName);
		}
	}
}

public class InvokableEvent<T0> : InvokableEventBase {

	public Action<T0> action;

	public void Invoke(T0 arg0) {
		action(arg0);
	}

	public override void Invoke(params object[] args) {
		action((T0) args[0]);
	}

	/// <summary> Constructor </summary>
	public InvokableEvent(object target, string methodName) {
		if (target == null || string.IsNullOrEmpty(methodName)) {
			action = x => { };
		} else {
			action = (System.Action<T0>) System.Delegate.CreateDelegate(typeof(System.Action<T0>), target, methodName);
		}
	}
}

public class InvokableEvent<T0, T1> : InvokableEventBase {

	public Action<T0, T1> action;

	public void Invoke(T0 arg0, T1 arg1) {
		action(arg0, arg1);
	}

	public override void Invoke(params object[] args) {
		action((T0) args[0], (T1) args[1]);
	}

	/// <summary> Constructor </summary>
	public InvokableEvent(object target, string methodName) {
		if (target == null || string.IsNullOrEmpty(methodName)) {
			action = (x, y) => { };
		} else {
			action = (System.Action<T0, T1>) System.Delegate.CreateDelegate(typeof(System.Action<T0, T1>), target, methodName);
		}
	}
}

public class InvokableEvent<T0, T1, T2> : InvokableEventBase {

	public Action<T0, T1, T2> action;

	public void Invoke(T0 arg0, T1 arg1, T2 arg2) {
		action(arg0, arg1, arg2);
	}

	public override void Invoke(params object[] args) {
		action((T0) args[0], (T1) args[1], (T2) args[2]);
	}

	/// <summary> Constructor </summary>
	public InvokableEvent(object target, string methodName) {
		if (target == null || string.IsNullOrEmpty(methodName)) {
			action = (x, y, z) => { };
		} else {
			action = (System.Action<T0, T1, T2>) System.Delegate.CreateDelegate(typeof(System.Action<T0, T1, T2>), target, methodName);
		}
	}
}

public class InvokableEvent<T0, T1, T2, T3> : InvokableEventBase {

	public Action<T0, T1, T2, T3> action;

	public void Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3) {
		action(arg0, arg1, arg2, arg3);
	}

	public override void Invoke(params object[] args) {
		action((T0) args[0], (T1) args[1], (T2) args[2], (T3) args[3]);
	}

	/// <summary> Constructor </summary>
	public InvokableEvent(object target, string methodName) {
		if (target == null || string.IsNullOrEmpty(methodName)) {
			action = (x, y, z, w) => { };
		} else {
			action = (System.Action<T0, T1, T2, T3>) System.Delegate.CreateDelegate(typeof(System.Action<T0, T1, T2, T3>), target, methodName);
		}
	}
}
