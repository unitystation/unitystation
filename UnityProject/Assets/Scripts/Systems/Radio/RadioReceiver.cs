using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class RadioReceiver : MonoBehaviour, IServerSpawn, IServerDespawn
{
	[SerializeField]
	[Tooltip("The frequency this device is waiting on a signal for. Uses MHz.")]
	private float frequency = 200f;
	public float Frequency => frequency;

	[SerializeField]
	[Tooltip("The mimimum frequency that will active a signal received call. Uses MHz.")]
	private float minFrequency = 0;
	public float MinFrequency => minFrequency;

	[SerializeField]
	[Tooltip("The maximum frequency that will active a signal received call. Uses MHz.")]
	private float maxFrequency = 0;
	public float MaxFrequency => maxFrequency;

	[SerializeField]
	private bool useFrequencyRange = false;
	public bool UseFrequencyRange => useFrequencyRange;

	[SerializeField]
	[Tooltip("This is the minimum intensity a signal can have if it is going to be picked up by this receiver. The intensity checked scales of the distance from the sender of the signal. Uses watts/m^2.")]
	private float sensitivity = 0.5f;
	public float Sensitivity => sensitivity;

	public bool IsOn = true;

	private RegisterTile registerTile;
	public RegisterTile RegisterTitle => registerTile;

	[SerializeField]
	public SignalReceivedEvent signalCallbacks;

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
	}

	public void OnReceiveSignal(RadioSignal signal)
	{
		signalCallbacks.Invoke(signal);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		RadioManager.Instance.RegisterReceiver(this);
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		RadioManager.Instance.RemoveReceiver(this);
	}
}

[System.Serializable]
public class SignalReceivedEvent : UnityEvent<RadioSignal> { }
