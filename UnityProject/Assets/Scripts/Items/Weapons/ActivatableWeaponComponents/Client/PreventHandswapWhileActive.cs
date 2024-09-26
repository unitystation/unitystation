namespace Items
{
	public enum HiddenHandValue
	{
		bothHands = 0,
		leftHand = 1,
		rightHand = 2,
		none = 3
	}
}

namespace Weapons.ActivatableWeapons
{
	public class PreventHandswapWhileActive : ClientActivatableWeaponComponent
	{
		private Pickupable pickupable;

		private void Start()
		{
			pickupable = GetComponent<Pickupable>();
			HandsController.OnSwapHand.AddListener(OnSwapHands);
		}

		private void OnDestroy()
		{
			HandsController.OnSwapHand.RemoveListener(OnSwapHands);
		}

		public override void ClientActivateBehaviour()
		{
			//
		}

		public override void ClientDeactivateBehaviour()
		{
			//
		}

		private void OnSwapHands()
		{
			if (av.IsActive == false) return;
			if (pickupable.ItemSlot == null) return;
			if (pickupable.ItemSlot.NamedSlot is not (NamedSlot.hands or NamedSlot.leftHand or NamedSlot.rightHand)) return;
			if (pickupable.ItemSlot.Player != PlayerManager.LocalPlayerScript.RegisterPlayer) return;

			Chat.AddExamineMsg(PlayerManager.LocalPlayerScript.gameObject,
				$"Your other hand is too busy holding {gameObject.ExpensiveName()}!");
				HandsController.OnSwapHand.RemoveListener(OnSwapHands);
				HandsController.SwapHand();
				HandsController.OnSwapHand.AddListener(OnSwapHands);
		}
	}
}