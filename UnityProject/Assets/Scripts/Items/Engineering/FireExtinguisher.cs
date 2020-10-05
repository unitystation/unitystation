using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Chemistry.Components;

[RequireComponent(typeof(Pickupable))]
public class FireExtinguisher : NetworkBehaviour,
	IInteractable<HandActivate>,
	ICheckedInteractable<AimApply>
{
	[SerializeField]
	[Range(1, 20)]
	private int travelDistance = 6;

	[SerializeField]
	[Range(1, 50)]
	private int reagentsPerUse = 1;

	[SerializeField]
	private ReagentContainer reagentContainer = default;

	private SpriteHandler spriteHandler;

	private float TravelTime => 1f / travelDistance;

	bool safety = true;
	private DateTime clientLastInteract = DateTime.Now;	

	private enum SpriteState
	{
		SafetyOn = 0,
		SafetyOff = 1
	}

	public void Awake()
	{
		spriteHandler = GetComponentInChildren<SpriteHandler>();
	}

	#region Interaction

	public void ServerPerformInteraction(HandActivate interaction)
	{
		safety = !safety;
		if (safety)
		{
			spriteHandler.ChangeSprite((int) SpriteState.SafetyOn);
		}
		else
		{
			spriteHandler.ChangeSprite((int) SpriteState.SafetyOff);
		}
	}

	public bool WillInteract(AimApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)
		    || (!IsCoolDown() && !isServer)) return false;
		return true;
	}

	public void ServerPerformInteraction(AimApply interaction)
	{
		if (reagentContainer.ReagentMixTotal < reagentsPerUse || safety) return;


		Vector2 startPos = gameObject.AssumedWorldPosServer();
		Vector2 targetPos = interaction.WorldPositionTarget.To2Int();
		List<Vector3Int> positionList = CheckPassableTiles(startPos, targetPos);
		StartCoroutine(Fire(positionList));

		var points = GetParallelPoints(startPos, targetPos, true);
		positionList = CheckPassableTiles(points[0], points[1]);
		StartCoroutine(Fire(positionList));

		points = GetParallelPoints(startPos, targetPos, false);
		positionList = CheckPassableTiles(points[0], points[1]);
		StartCoroutine(Fire(positionList));

		Effect.PlayParticleDirectional(this.gameObject, interaction.TargetVector);

		SoundManager.PlayNetworkedAtPos("Extinguish", startPos, 1, sourceObj: interaction.Performer);

		interaction.Performer.Pushable()?.NewtonianMove((-interaction.TargetVector).NormalizeToInt());
	}

	#endregion Interaction;

	/// <summary>
	/// Returns the vectors that form a line parallel to the arguments
	/// </summary>
	private Vector2[] GetParallelPoints(Vector2 startPos, Vector2 targetPos, bool rightSide)
	{
		Vector2 difference = targetPos - startPos;
		Vector2 rotated = Vector2.Perpendicular(difference).normalized;
		Vector2 paralelStart;
		Vector2 paralelTarget;
		if (rightSide)
		{
			paralelStart = startPos - rotated;
			paralelTarget = startPos - rotated + difference;
		}
		else
		{
			paralelStart = startPos + rotated;
			paralelTarget = startPos + rotated + difference;
		}

		paralelStart = new Vector2(Mathf.RoundToInt(paralelStart.x), Mathf.RoundToInt(paralelStart.y));
		paralelTarget = new Vector2(Mathf.RoundToInt(paralelTarget.x), Mathf.RoundToInt(paralelTarget.y));
		var points = new Vector2[] { paralelStart, paralelTarget };
		return points;
	}

	private IEnumerator Fire(List<Vector3Int> positionList)
	{
		for (int i = 0; i < positionList.Count; i++)
		{
			ExtinguishTile(positionList[i]);
			yield return WaitFor.Seconds(TravelTime);
		}
	}

	private void ExtinguishTile(Vector3Int worldPos)
	{
		reagentContainer.Spill(worldPos, reagentsPerUse);
	}

	private List<Vector3Int> CheckPassableTiles(Vector2 startPos, Vector2 targetPos)
	{
		List<Vector3Int> passableTiles = new List<Vector3Int>();
		List<Vector3Int> positionList = MatrixManager.GetTiles(startPos, targetPos, travelDistance);
		for (int i = 0; i < positionList.Count; i++)
		{
			if (!MatrixManager.IsAtmosPassableAt(positionList[i], true))
			{
				return passableTiles;
			}
			passableTiles.Add(positionList[i]);
		}
		return passableTiles;
	}

	private bool IsCoolDown()
	{
		var totalSeconds = (DateTime.Now - clientLastInteract).TotalSeconds;
		if (totalSeconds < 1f)
		{
			return false;
		}

		clientLastInteract = DateTime.Now;
		return true;
	}
}
