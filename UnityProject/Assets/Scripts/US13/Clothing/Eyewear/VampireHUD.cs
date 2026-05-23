using Mirror;
using UnityEngine;
using US13.Player;
using US13.Player.HUDData;
using Util;

namespace US13.Clothing.Eyewear
{
	public class VampireHUD : NetworkBehaviour, IHUD
	{
		[field:SerializeField]
		public GameObject Prefab { get; set; }

		public GameObject InstantiatedGameObject { get; set; }

		private VampireHUDHandler vampireHUDHandler;
		private PlayerScript playerScript;

		public HUDHandler HUDHandler;

		[SyncVar(hook = nameof(SyncCurrentStage))] private int currentStage;

		public void Awake()
		{
			playerScript =  this.GetCachedComponent<PlayerScript>();
			HUDHandler = this.GetCachedComponent<HUDHandler>();
			HUDHandler.AddNewHud(this);
		}


		public void SetUp()
		{
			vampireHUDHandler = InstantiatedGameObject.GetComponent<VampireHUDHandler>();
			vampireHUDHandler.UpdateStage(-1);

			var visibility = false;
			var ThisType = typeof(VampireHUD);
			if (HUDHandler.CategoryEnabled.ContainsKey(ThisType)) //So if you join mid round you still have the HUD showing
			{
				visibility = HUDHandler.CategoryEnabled[ThisType];
			}
			vampireHUDHandler.SetVisible(visibility);
		}

		public void SetVisible(bool Visible)
		{
			if (playerScript.ObjectPhysics.Intangible)
			{
				Visible = false;
			}
			vampireHUDHandler.SetVisible(Visible);
		}

		public void SyncCurrentStage(int oldStage, int newStage)
		{
			vampireHUDHandler.UpdateStage(newStage);
			currentStage = newStage;
		}

		public void OnDestroy()
		{
			HUDHandler.RemoveHud(this);
		}
	}
}
