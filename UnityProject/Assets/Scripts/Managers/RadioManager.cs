using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Serverside radio management tool. Clients cannot send radio messages! If you wish to send a radio message, call it on the server!
/// </summary>
public class RadioManager : MonoBehaviour
{
	private static RadioManager radioManager;


	public static RadioManager Instance
	{
		get
		{
			if (!radioManager)
			{
				radioManager = FindObjectOfType<RadioManager>();
			}

			return radioManager;
		}
	}


	private List<RadioReceiver> receivers = new List<RadioReceiver>();

	public void RegisterReceiver(RadioReceiver rec)
	{
		receivers.Add(rec);
	}

	public void RemoveReceiver(RadioReceiver rec)
	{
		receivers.Remove(rec);
	}

	/// <summary>
	/// Check if a receiver can receive a signal from a messager. Probably a better way to do this.
	/// </summary>
	/// <param name="rec"></param>
	/// <param name="signal"></param>
	/// <param name="originator"></param>
	/// <returns></returns>

	//TODO
	//Degrade the message in a sent signal if it's below the sensitivity of the receiver (up to a certain range).
	//i.e. add corrupted text and stuff

	private bool CanReceiverReceiverSignal(RadioReceiver rec, RadioSignal signal, RadioMessager originator)
	{
		//Prevent a receiver picking up a signal if it was the one that sent it.
		if (rec.gameObject.Equals(originator.gameObject)) return false;

		if (signal.broadRange)
		{
			if (rec.UseFrequencyRange)
			{
				if (signal.frequencyMin >= rec.MinFrequency || signal.frequencyMax <= rec.MaxFrequency)
				{
					float distance = Vector3.Distance(rec.RegisterTitle.WorldPosition, originator.RegisterTitle.WorldPosition);
					if (signal.intensity / (Mathf.Pow(distance, 2)) >= rec.Sensitivity)
					{
						return true;
					}
				}
			}
			else
			{
				if (signal.frequencyMin < rec.Frequency && signal.frequencyMax > rec.Frequency)
				{
					float distance = Vector3.Distance(rec.RegisterTitle.WorldPosition, originator.RegisterTitle.WorldPosition);
					if (signal.intensity / (Mathf.Pow(distance, 2)) >= rec.Sensitivity)
					{
						return true;
					}
				}

			}
		}
		else
		{
			if (rec.UseFrequencyRange)
			{
				if (signal.frequency >= rec.MinFrequency && signal.frequency <= rec.MaxFrequency)
				{
					float distance = Vector3.Distance(rec.RegisterTitle.WorldPosition, originator.RegisterTitle.WorldPosition);
					if (signal.intensity / (Mathf.Pow(distance, 2)) >= rec.Sensitivity)
					{
						return true;
					}
				}
			}
			else
			{
				//Have to use an epsilon check here, since floats are imperfect.
				if (Mathf.Abs(signal.frequency - rec.Frequency) < 0.01)
				{
					float distance = Vector3.Distance(rec.RegisterTitle.WorldPosition, originator.RegisterTitle.WorldPosition);
					if (signal.intensity / (Mathf.Pow(distance, 2)) >= rec.Sensitivity)
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	public void SendRadioSignal(RadioSignal signal, RadioMessager originator)
	{
		foreach (RadioReceiver rec in receivers)
		{
			if (CanReceiverReceiverSignal(rec, signal, originator))
			{
				rec.OnReceiveSignal(signal);
			}
		}
	}

	public void SendRadioSignalFromLocation(RadioSignal signal, Vector2 origin)
	{

	}
}

/// <summary>
/// These are the signals that will be sent out with the radio manager. They have a specified frequency, amplitude, intensity and message.
/// Frequencies use MHz, amplitude uses watts, intensity uses watts/m^2.
/// </summary>
public class RadioSignal
{
	public float frequency { get; private set; }
	public float frequencyMin { get; private set; }
	public float frequencyMax { get; private set; }
	public float amplitude { get; private set; }
	public float intensity { get; private set; }
	public bool broadRange { get; private set; }
	public string message { get; private set; }

	//Message is null by default. For signals that only care about I/O.
	public RadioSignal(float frequency, float amplitude, float intensity, string message = null)
	{
		this.frequency = frequency;
		this.amplitude = amplitude;
		this.intensity = intensity;
		this.broadRange = false;
		this.message = message;
	}

	public RadioSignal(float frequencyMin, float frequencyMax, float amplitude, float intensity, string message = null)
	{
		this.frequencyMin = frequencyMin;
		this.frequencyMax = frequencyMax;
		this.amplitude = amplitude;
		this.intensity = intensity;
		this.broadRange = true;
		this.message = message;
	}
}

