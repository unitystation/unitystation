using UnityEngine;

public class AtmosManager : MonoBehaviour
{
	public float Speed = 0.1f;

	public int NumberThreads = 1;

	public AtmosMode Mode = AtmosMode.Threaded;

	public bool Running { get; private set; }

	private void OnValidate()
	{
		AtmosThread.SetSpeed(Speed);

		// TODO set number of threads
	}

	private void Start()
	{
		if (Mode != AtmosMode.Manual)
		{
			StartSimulation();
		}
	}

	private void Update()
	{
		if (Mode == AtmosMode.GameLoop && Running)
		{
			AtmosThread.RunStep();
		}
	}

	private void OnApplicationQuit()
	{
		StopSimulation();
	}

	public void StartSimulation()
	{
		Running = true;

		if (Mode == AtmosMode.Threaded)
		{
			AtmosThread.Start();
		}
	}

	public void StopSimulation()
	{
		Running = false;

		AtmosThread.Stop();
	}

	public static void Update(MetaDataNode node)
	{
		AtmosThread.Enqueue(node);
	}
}

public enum AtmosMode
{
	Threaded,
	GameLoop,
	Manual
}