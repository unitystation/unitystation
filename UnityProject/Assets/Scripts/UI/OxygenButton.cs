using UnityEngine;
using UnityEngine.UI;


public class OxygenButton : MonoBehaviour
{
	private Image image;
	public Sprite[] stateSprites;
	public bool IsInternalsEnabled;

	// Use this for initialization
	private void Start()
	{
		image = GetComponent<Image>();
		IsInternalsEnabled = false;
		EventManager.AddHandler(EVENT.EnableInternals, OnEnableInternals);
		EventManager.AddHandler(EVENT.DisableInternals, OnDisableInternals);
	}

	private void OnDestroy()
	{
		EventManager.RemoveHandler(EVENT.EnableInternals, OnEnableInternals);
		EventManager.RemoveHandler(EVENT.DisableInternals, OnDisableInternals);
	}

	/// <summary>
	/// toggle the button state and play any sounds
	/// </summary>
	public void OxygenSelect()
	{
		SoundManager.Play("Click01");

		//toggle state
		IsInternalsEnabled = !IsInternalsEnabled;
		
		if (IsInternalsEnabled)
		{
			EventManager.Broadcast(EVENT.EnableInternals);
		}
		else
		{
			EventManager.Broadcast(EVENT.DisableInternals);
		}
	}

	public void OnEnableInternals()
	{
		image.sprite = stateSprites[1];
	}

	public void OnDisableInternals()
	{
		image.sprite = stateSprites[0];
	}
}
