using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// This handles opening a network tab internally, and hence, doesn't require a HasNetworkTab component. Don't add one!
/// </summary>
public class HackingProcessDoorSimple : HackingProcessBase
{
	public NetTabType NetTabType = NetTabType.HackingPanel;

	private static int? doorSeed = null;
	private static int? DoorSeed
	{
		get
		{
			if (doorSeed == null)
			{
				doorSeed = Random.RandomRange(1, 100000);
			}
			return doorSeed;
		}
	}


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

		if (interaction.HandObject == null && !WiresExposed) return false;

		return IntDoor != null && IntDoor.allowInput;
	}

	public override void ClientPredictInteraction(HandApply interaction)
	{
	}

	public override void ServerRollbackClient(HandApply interaction) { }

	public override void ServerPerformInteraction(HandApply interaction)
	{

		//Do specific things when the wires are exposed.
		if (WiresExposed)
		{
			if (interaction.HandObject == null && interaction.Performer != null)
			{
				TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType, TabAction.Open);
				IntDoor.StartInputCoolDown();
				return;
			}
		}

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

				IntDoor.StartInputCoolDown();
				return;
			}

		}

	}

	public override void ServerGenerateNodesFromNodeInfo()
	{
		foreach (HackingNodeInfo inf in nodeInfo.nodeInfoList.ToList().Shuffle())
		{
			HackingNode newNode = new HackingNode();
			newNode.IsInput = inf.IsInput;
			newNode.IsOutput = inf.IsOutput;
			newNode.IsDeviceNode = inf.IsDeviceNode;
			newNode.InternalIdentifier = inf.InternalIdentifier;
			newNode.HiddenLabel = inf.HiddenLabel;
			newNode.PublicLabel = inf.PublicLabel;

			hackNodes.Add(newNode);
		}
	}

	public override void ServerLinkHackingNodes()
	{
		Controller.LinkHackNodes();
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

	public override void OnDespawnServer(DespawnInfo info)
	{
		NetworkTabManager.Instance.RemoveTab(gameObject, NetTabType);
	}

}
