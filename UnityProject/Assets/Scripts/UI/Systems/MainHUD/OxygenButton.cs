using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OxygenButton : TooltipMonoBehaviour
{
	private Image image;
	public Sprite[] stateSprites;
	public bool IsInternalsEnabled;
	public override string Tooltip => "toggle internals";

	void Awake()
	{
		image = GetComponent<Image>();
		IsInternalsEnabled = false;
	}

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.EnableInternals, OnEnableInternals);
		EventManager.AddHandler(EVENT.DisableInternals, OnDisableInternals);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.EnableInternals, OnEnableInternals);
		EventManager.RemoveHandler(EVENT.DisableInternals, OnDisableInternals);
	}

	/// <summary>
	/// toggle the button state and play any sounds
	/// </summary>
	public void OxygenSelect()
	{
		if (PlayerManager.LocalPlayer == null)
		{
			return;
		}

		if (PlayerManager.LocalPlayerScript.playerHealth.IsCrit)
		{
			return;
		}

		SoundManager.Play(SingletonSOSounds.Instance.Click01);

		if (IsInternalsEnabled)
		{
			EventManager.Broadcast(EVENT.DisableInternals);
		}
		else
		{
			EventManager.Broadcast(EVENT.EnableInternals);
		}
	}

	public void OnEnableInternals()
	{
		image.sprite = stateSprites[1];
		IsInternalsEnabled = true;
	}

	public void OnDisableInternals()
	{
		image.sprite = stateSprites[0];
		IsInternalsEnabled = false;
	}
}