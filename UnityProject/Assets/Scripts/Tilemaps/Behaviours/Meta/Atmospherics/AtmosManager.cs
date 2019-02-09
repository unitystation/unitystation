using UnityEngine;


public class AtmosManager : MonoBehaviour
{
	[Tooltip("frequency of atmos simulation updates (seconds between each update)")]
	public float Speed = 0.1f;

	[Tooltip("not currently implemented, thread count is always locked at one regardless of this setting")]
	public int NumberThreads = 1;

	private void OnValidate()
	{
		AtmosThread.SetSpeed(Speed);

		// TODO set number of threads
	}

}