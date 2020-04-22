using UnityEngine;

public class ShroudTile : MonoBehaviour
{
	public new Renderer renderer;

	private void OnEnable()
	{
		renderer.enabled = true;
	}

	public void SetShroudStatus(bool enabled)
	{
		renderer.enabled = enabled;
	}
}