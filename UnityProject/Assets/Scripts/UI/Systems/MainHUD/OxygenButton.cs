using UnityEngine;
using UnityEngine.UI;

namespace UI
{
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
			EventManager.AddHandler(Event.EnableInternals, OnEnableInternals);
			EventManager.AddHandler(Event.DisableInternals, OnDisableInternals);
		}

		void OnDisable()
		{
			EventManager.RemoveHandler(Event.EnableInternals, OnEnableInternals);
			EventManager.RemoveHandler(Event.DisableInternals, OnDisableInternals);
		}

		/// <summary>
		/// Toggle the button state and play any sounds
		/// </summary>
		public void OxygenSelect()
		{
			if (PlayerManager.LocalPlayer == null) return;
			if (PlayerManager.LocalPlayerScript.playerHealth.IsCrit) return;

			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			EventManager.Broadcast(IsInternalsEnabled ? Event.DisableInternals : Event.EnableInternals);
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
}
