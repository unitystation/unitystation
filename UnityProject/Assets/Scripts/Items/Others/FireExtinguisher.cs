using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Pickupable))]
public class FireExtinguisher : NetworkBehaviour, IInteractable<HandActivate>, ICheckedInteractable<AimApply>
{
	bool safety = true;
	int travelDistance = 6;
	public ReagentContainer reagentContainer;
	public RegisterItem registerItem;
	public Pickupable pickupable;

	public SpriteRenderer spriteRenderer;
	[SyncVar(hook = nameof(SyncSprite))] public int spriteSync;
	public Sprite[] spriteList;

	public override void OnStartClient()
	{
		SyncSprite(spriteSync);
		base.OnStartClient();
	}

	public void Awake()
	{
		if ( !pickupable )
		{
			pickupable = GetComponent<Pickupable>();
		}
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (safety)
		{
			safety = false;
			SyncSprite(1);
		}
		else
		{
			safety = true;
			SyncSprite(0);
		}
	}

	public bool WillInteract(AimApply interaction, NetworkSide side)
	{
		if (interaction.MouseButtonState == MouseButtonState.PRESS)
		{
			return true;
		}

		return false;
	}

	public void ServerPerformInteraction(AimApply interaction)
	{
		if (reagentContainer.CurrentCapacity >= 5 && !safety)
		{
			Vector2	startPos = gameObject.AssumedWorldPosServer();
			Vector2 targetPos = interaction.WorldPositionTarget.To2Int();
			List<Vector3Int> positionList = MatrixManager.GetTiles(startPos, targetPos, travelDistance);
			StartCoroutine(Fire(positionList));

			var points = GetParallelPoints(startPos, targetPos, true);
			positionList = MatrixManager.GetTiles(points[0], points[1], travelDistance);
			StartCoroutine(Fire(positionList));

			points = GetParallelPoints(startPos, targetPos, false);
			positionList = MatrixManager.GetTiles(points[0], points[1], travelDistance);
			StartCoroutine(Fire(positionList));

			Effect.PlayParticleDirectional( this.gameObject, interaction.TargetVector );

			SoundManager.PlayNetworkedAtPos("Extinguish", startPos, 1);
			reagentContainer.MoveReagentsTo(5);
		}
	}

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
		var points = new Vector2[] {paralelStart, paralelTarget};
		return points;
	}

	private IEnumerator Fire(List<Vector3Int> positionList)
	{
		for (int i = 0; i < positionList.Count; i++)
		{
			ExtinguishTile(positionList[i]);
			yield return WaitFor.Seconds(0.17f);
		}
	}

	void ExtinguishTile(Vector3Int worldPos)
	{
		var matrix = MatrixManager.AtPoint(worldPos, true);
		var localPosInt = MatrixManager.WorldToLocalInt(worldPos, matrix);
		matrix.MetaDataLayer.ReagentReact(reagentContainer.Contents, worldPos, localPosInt);
	}

	public void SyncSprite(int value)
	{
		spriteSync = value;
		spriteRenderer.sprite = spriteList[spriteSync];

		pickupable.RefreshUISlotImage();
	}

}