[System.Serializable]
public class SerializableEvent : SerializableEventBase {
	public void Invoke() {
		if (invokable == null) Cache();
		if (_dynamic) {
			InvokableEvent call = invokable as InvokableEvent;
			call.Invoke();
		} else {
			invokable.Invoke(Args);
		}
	}

	protected override void Cache() {
		if (_target == null || string.IsNullOrEmpty(_methodName)) {
			invokable = new InvokableEvent(null, null);
		} else {
			if (_dynamic) {
				invokable = new InvokableEvent(target, methodName);
			} else {
				invokable = GetPersistentMethod();
			}
		}
	}
}

public abstract class SerializableEvent<T0> : SerializableEventBase {
	public void Invoke(T0 arg0) {
		if (invokable == null) Cache();
		if (_dynamic) {
			InvokableEvent<T0> call = invokable as InvokableEvent<T0>;
			call.Invoke(arg0);
		} else {
			invokable.Invoke(Args);
		}
	}

	protected override void Cache() {
		if (_target == null || string.IsNullOrEmpty(_methodName)) {
			invokable = new InvokableEvent<T0>(null, null);
		} else {
			if (_dynamic) {
				invokable = new InvokableEvent<T0>(target, methodName);
			} else {
				invokable = GetPersistentMethod();
			}
		}
	}
}

public abstract class SerializableEvent<T0, T1> : SerializableEventBase {
	public void Invoke(T0 arg0, T1 arg1) {
		if (invokable == null) Cache();
		if (_dynamic) {
			InvokableEvent<T0, T1> call = invokable as InvokableEvent<T0, T1>;
			call.Invoke(arg0, arg1);
		} else {
			invokable.Invoke(Args);
		}
	}

	protected override void Cache() {
		if (_target == null || string.IsNullOrEmpty(_methodName)) {
			invokable = new InvokableEvent<T0, T1>(null, null);
		} else {
			if (_dynamic) {
				invokable = new InvokableEvent<T0, T1>(target, methodName);
			} else {
				invokable = GetPersistentMethod();
			}
		}
	}
}

public abstract class SerializableEvent<T0, T1, T2> : SerializableEventBase {
	public void Invoke(T0 arg0, T1 arg1, T2 arg2) {
		if (invokable == null) Cache();
		if (_dynamic) {
			InvokableEvent<T0, T1, T2> call = invokable as InvokableEvent<T0, T1, T2>;
			call.Invoke(arg0, arg1, arg2);
		} else {
			invokable.Invoke(Args);
		}
	}

	protected override void Cache() {
		if (_target == null || string.IsNullOrEmpty(_methodName)) {
			invokable = new InvokableEvent<T0, T1, T2>(null, null);
		} else {
			if (_dynamic) {
				invokable = new InvokableEvent<T0, T1, T2>(target, methodName);
			} else {
				invokable = GetPersistentMethod();
			}
		}
	}
}

public abstract class SerializableEvent<T0, T1, T2, T3> : SerializableEventBase {
	public void Invoke(T0 arg0, T1 arg1, T2 arg2, T3 arg3) {
		if (invokable == null) Cache();
		if (_dynamic) {
			InvokableEvent<T0, T1, T2, T3> call = invokable as InvokableEvent<T0, T1, T2, T3>;
			call.Invoke(arg0, arg1, arg2, arg3);
		} else {
			invokable.Invoke(Args);
		}
	}

	protected override void Cache() {
		if (_target == null || string.IsNullOrEmpty(_methodName)) {
			invokable = new InvokableEvent<T0, T1, T2, T3>(null, null);
		} else {
			if (_dynamic) {
				invokable = new InvokableEvent<T0, T1, T2, T3>(target, methodName);
			} else {
				invokable = GetPersistentMethod();
			}
		}
	}
}