using UnityEngine;

/// <summary>
/// Dynamic list entry
/// </summary>
public class DynamicEntry : NetUIElement {
	public NetUIElement[] Elements => GetComponentsInChildren<NetUIElement>(false);
	public override ElementMode InteractionMode => ElementMode.ServerWrite;

	public override string Value {
		get {
			return ((Vector2)transform.localPosition).Stringified();
		}
		set {
			externalChange = true;
			transform.localPosition = value.Vectorized();
			externalChange = false;
		}
	}

	public Vector3 Position {
		get { return transform.localPosition; }
		set { transform.localPosition = value; }
	}

	public override void ExecuteServer() {}
}