using UnityEngine;

public class SimpleImageRotate : MonoBehaviour
{
	private bool rotating = false;

	[TooltipAttribute("Degrees per second")]
	public float Speed = 180;

	public bool randomise = false;

	private void OnEnable()
	{
		if (randomise) Speed = Random.Range(-180f, 180f);
		rotating = true;
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		rotating = false;
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void UpdateMe()
	{
		if (rotating)
		{
			transform.Rotate(0, 0, Speed * Time.deltaTime);
		}
	}
}