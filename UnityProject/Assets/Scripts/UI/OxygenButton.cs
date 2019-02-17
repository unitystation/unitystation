using UnityEngine;
using UnityEngine.UI;


public class OxygenButton : MonoBehaviour
{
	private Image image;
	public Sprite[] stateSprites;

	// Use this for initialization
	private void Start()
	{
		image = GetComponent<Image>();
		UIManager.IsOxygen = false;
	}

	/// <summary>
	/// toggle the button state and play any sounds
	/// </summary>
	public void OxygenSelect()
	{
		SoundManager.Play("Click01");
		//toggle state
		EnableOxygen(!UIManager.IsOxygen);
	}

	/// <summary>
	/// Sets the state of the OxygenButton.
	/// </summary>
	/// <param name="enableOxygen"></param>
	public void EnableOxygen(bool enableOxygen)
	{
		UIManager.IsOxygen = enableOxygen;
		//PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetInternalsEnabled(enableOxygen);
		if (enableOxygen)
		{
			image.sprite = stateSprites[1];
			EventManager.Broadcast(EVENT.EnableInternals);
		}
		else
		{
			image.sprite = stateSprites[0];
			EventManager.Broadcast(EVENT.DisableInternals);
		}

	}
}
