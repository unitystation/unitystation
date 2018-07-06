using UnityEngine;
public class SimpleImageRotate : MonoBehaviour {
	private bool rotating = false;
	[TooltipAttribute("Degrees per second")]
	public float Speed = 180;
	public bool Clockwise = true;
	private void OnEnable() {
		rotating = true;
	}

	private void Update() {
		if ( rotating ) {
			transform.Rotate( 0, 0, Speed * Time.deltaTime * (Clockwise ? -1 : 1) );
		}
	}

	private void OnDisable() {
		rotating = false;
	}
}
