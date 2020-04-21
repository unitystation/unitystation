using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HackingProcessDoorSimple : HackingProcessBase
{

	[SerializeField]
	[Tooltip("The name that comes up when you interact with the object.")]
	private string doorName = "airlock";

	[SerializeField]
	[Tooltip("SpriteRender which is toggled on when the door panel is exposed.")]
	private SpriteRenderer hackPanelOverlay = null;

	[SerializeField]
	private Sprite hackPanelSprite = null;

	private DoorController controller;
	public DoorController Controller
	{
		get
		{
			if (!controller)
			{
				controller = GetComponent<DoorController>();
			}
			return controller;
		}
	}

	private InteractableDoor intDoor;
	public InteractableDoor IntDoor
	{
		get
		{
			if (!intDoor)
			{
				intDoor = GetComponent<InteractableDoor>();
			}
			return intDoor;
		}
	}

	public override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (interaction.TargetObject != gameObject) return false;

		return IntDoor != null && IntDoor.allowInput;
	}

	public override void ClientPredictInteraction(HandApply interaction)
	{
		IntDoor.StartInputCoolDown();
	}

	public override void ServerRollbackClient(HandApply interaction) { }

	public override void ServerPerformInteraction(HandApply interaction)
	{

		//Do specific things when the wires are exposed.
		if (WiresExposed)
		{

		}
		//Note, if the wires are exposed and an action is taken, then we should probably return in there, shouldn't also be running these options.

		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver))
		{

			var screwdriver = interaction.HandObject.GetComponent<Screwdriver>();
			if (interaction.Intent != Intent.Help)
			{
				if (Controller != null)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer,
						"You " + (WiresExposed ? "close" : "open") + " the " + doorName + "'s maintenance panel");
					ServerTryTogglePanel();
				}

				return;
			}

		}

		IntDoor.StartInputCoolDown();

	}

	public void ServerTryTogglePanel()
	{
		if (!Controller.isPerformingAction)
		{
			ToggleWiresExposed();
		}
	}

	protected override void OnWiresExposed()
	{
		if (hackPanelOverlay != null && hackPanelSprite != null)
		{
			hackPanelOverlay.sprite = hackPanelSprite;
		}
	}

	protected override void OnWiresHidden()
	{
		if (hackPanelOverlay != null)
		{
			hackPanelOverlay.sprite = null;
		}
	}

	public override void CreateHackPrefab()
	{
		throw new System.NotImplementedException();
	}
}
