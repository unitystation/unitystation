using System;
using System.Collections.Generic;
using Items;
using AddressableReferences;
using Mirror;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Weapons;

[RequireComponent(typeof(ItemAttributesV2))]
[RequireComponent(typeof(Gun))]
public class SawnOff : MonoBehaviour, ICheckedInteractable<InventoryApply>
{

	private ItemAttributesV2 itemAttComp =>
		gameObject.GetComponent<ItemAttributesV2>();

	private bool isSawn = false;

	[SerializeField, Tooltip("Determines if the gun will explode in the users face when sawn while it still contains ammo")]
	private bool ammoBackfire = true;

	private Gun gunComp =>
		gameObject.GetComponent<Gun>();

	private SpriteHandler spriteHandler;

	[SerializeField]
	private ItemSize sawnSize = ItemSize.Medium;
	
	[SerializeField]
	private float sawnMaxRecoilVariance;

	[SerializeField]
	private CameraRecoilConfig SawnCameraRecoilConfig;

	private void Awake()
	{
		spriteHandler = GetComponentInChildren<SpriteHandler>();
		spriteHandler.ChangeSprite(0);
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (side == NetworkSide.Server && DefaultWillInteract.Default(interaction, side)) return true;

		//only reload if the gun is the target and mag/clip is in hand slot
		if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot && side == NetworkSide.Client)
		{
			//TODO: switch this trait to the circular saw when that is implemented
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder))
			{
				gunComp.CameraRecoilConfig = SawnCameraRecoilConfig;
				return true;
			}
		}
		return false;
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		if (interaction.TargetObject == gameObject && interaction.IsFromHandSlot)
		{
			//TODO: switch this trait to the circular saw when that is implemented
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder) && gunComp.FireCountDown == 0)
			{
				if (isSawn)
				{
					Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} is already shortened!");
				}

				WaitFor.Seconds(0.25f);
				if (ammoBackfire && gunComp.CurrentMagazine.ServerAmmoRemains != 0)
				{
					gunComp.ServerShoot(interaction.Performer, Vector2.zero, BodyPartType.Head, true);
					Chat.AddActionMsgToChat(interaction.Performer,
					$"The {gameObject.ExpensiveName()} goes off in  your face!",
					$"The {gameObject.ExpensiveName()} goes off in {interaction.Performer.ExpensiveName()}'s face!");
				}
				else
				{
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You shorten the {gameObject.ExpensiveName()}.",
						$"{interaction.Performer.ExpensiveName()} shortens the {gameObject.ExpensiveName()}");

					itemAttComp.ServerSetSize(sawnSize);
   					spriteHandler.ChangeSprite(1);
   					gunComp.CameraRecoilConfig = SawnCameraRecoilConfig;
   					gunComp.MaxRecoilVariance = sawnMaxRecoilVariance;
					isSawn = true;
				}
			}
		}
	}
}