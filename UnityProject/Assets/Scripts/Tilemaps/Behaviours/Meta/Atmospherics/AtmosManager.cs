using UnityEngine;


public class AtmosManager : MonoBehaviour
{
	public float Speed = 0.1f;

	public int NumberThreads = 1;

	private void OnValidate()
	{
		AtmosThread.SetSpeed(Speed);

		// TODO set number of threads
	}

}