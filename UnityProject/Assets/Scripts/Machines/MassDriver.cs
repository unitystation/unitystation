using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

[RequireComponent(typeof(GeneralSwitchController))]
[ExecuteInEditMode]
public class MassDriver : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	private const float ANIMATION_TIME = 0.5f;

	private RegisterTile registerTile;

	[SerializeField]
	private Directional directional;

	private Matrix Matrix => registerTile.Matrix;

	private SpriteHandler spriteHandler;

	private GeneralSwitchController switchController;

	bool massDriverOperating = false;

	[SyncVar(hook = nameof(OnSyncOrientation))]
	private Orientation orientation;

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
		spriteHandler = GetComponentInChildren<SpriteHandler>();
		switchController = GetComponent<GeneralSwitchController>();

		if (directional != null)
		{
			directional.OnDirectionChange.AddListener(OnDirectionChanged);
		}
	}

	private void OnEnable()
	{
		switchController.SwitchPressedDoAction.AddListener(DoAction);
	}

	private void OnDisable()
	{
		switchController.SwitchPressedDoAction.RemoveListener(DoAction);
	}

	#region Sync

	private void UpdateSpriteOutletState()
	{
		if (massDriverOperating) spriteHandler.ChangeSprite(1);
		else spriteHandler.ChangeSprite(0);
	}

	private void OnDirectionChanged(Orientation newDir)
	{
		orientation = newDir;
	}

	private void OnSyncOrientation(Orientation oldState, Orientation newState)
	{
		orientation = newState;
		UpdateSpriteOrientation();
	}

	public void EditorUpdate()
	{
		orientation = directional.InitialOrientation;
		UpdateSpriteOrientation();
	}

	private void UpdateSpriteOrientation()
	{
		switch (orientation.AsEnum())
		{
			case OrientationEnum.Up:
				spriteHandler.ChangeSpriteVariant(1);
				break;
			case OrientationEnum.Down:
				spriteHandler.ChangeSpriteVariant(0);
				break;
			case OrientationEnum.Left:
				spriteHandler.ChangeSpriteVariant(3);
				break;
			case OrientationEnum.Right:
				spriteHandler.ChangeSpriteVariant(2);
				break;
		}
	}
	#endregion

	public void DoAction()
	{
		if(!CustomNetworkManager.IsServer) return;

		StartCoroutine(DetectObjects());
	}

	private IEnumerator DetectObjects()
	{
		massDriverOperating = true;
		UpdateSpriteOutletState();
		//detect players positioned on the mass driver
		var playersFound = Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, ObjectType.Player, true);

		var throwVector = orientation.Vector;

		foreach (ObjectBehaviour player in playersFound)
		{
			// Players cannot currently be thrown, so just push them in the direction for now.
			PushPlayer(player, throwVector);
		}

		foreach (var objects in Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, ObjectType.Object, true))
		{
			// Objects cannot currently be thrown, so just push them in the direction for now.
			PushObject(objects, throwVector);
		}

		foreach (var item in Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, ObjectType.Item, true))
		{
			ThrowItem(item, throwVector);
		}

		yield return WaitFor.Seconds(ANIMATION_TIME);

		massDriverOperating = false;
		UpdateSpriteOutletState();
	}

	private void ThrowItem(ObjectBehaviour item, Vector3 throwVector)
    {
    	Vector3 vector = item.transform.rotation * throwVector;
        var spin = RandomSpin();
    	ThrowInfo throwInfo = new ThrowInfo
    	{
    		ThrownBy = gameObject,
    		Aim = BodyPartType.Chest,
    		OriginWorldPos = transform.position,
    		WorldTrajectory = vector,
    		SpinMode = spin
    	};

    	CustomNetTransform itemTransform = item.GetComponent<CustomNetTransform>();
    	if (itemTransform == null) return;
    	itemTransform.Throw(throwInfo);
    }

	private SpinMode RandomSpin()
	{
		var num = Random.Range(0,3);

		switch (num)
		{
			case 0:
				return SpinMode.None;
			case 1:
				return SpinMode.Clockwise;
			case 2:
				return SpinMode.CounterClockwise;
			default:
				return SpinMode.Clockwise;
		}
	}

	private void PushObject(ObjectBehaviour entity, Vector3 pushVector)
    {
	    //Push Twice
    	entity.QueuePush(pushVector.NormalizeTo2Int());
        entity.QueuePush(pushVector.NormalizeTo2Int());
    }

	private void PushPlayer(ObjectBehaviour player, Vector3 pushVector)
    {
    	player.GetComponent<RegisterPlayer>()?.ServerStun();

        //Push Twice
    	player.QueuePush(pushVector.NormalizeTo2Int());
        player.QueuePush(pushVector.NormalizeTo2Int());
    }

	#region WrenchChangeDirection

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (Validations.IsTarget(gameObject, interaction)) return true;

		return false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.HandObject == null) return;

		if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wrench))
		{
			switch (orientation.AsEnum())
			{
				case OrientationEnum.Right:
					directional.FaceDirection(Orientation.Up);
					break;
				case OrientationEnum.Up:
					directional.FaceDirection(Orientation.Left);
					break;
				case OrientationEnum.Left:
					directional.FaceDirection(Orientation.Down);
					break;
				case OrientationEnum.Down:
					directional.FaceDirection(Orientation.Right);
					break;
			}
		}
	}

	#endregion
}
