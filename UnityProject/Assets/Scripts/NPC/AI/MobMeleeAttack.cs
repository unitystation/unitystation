using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic AI behaviour for following and melee attacking a target
/// </summary>
[RequireComponent(typeof(MobAI))]
public class MobMeleeAttack : MobFollow
{
	[Tooltip("The sprites gameobject. Needs to be a child of the prefab root")]
	public GameObject spriteHolder;
	[Tooltip("If a player gets close to this mob and blocks the mobs path to the target," +
	         "should the mob then focus on the human blocking it?. Only works if mob is targeting" +
	         "a player originally.")]
	public bool targetOtherPlayersWhoGetInWay;

	private LayerMask checkMask;
	private int playersLayer;
	private int npcLayer;
	private int windowsLayer;

	private bool isForLerpBack;
	private Vector3 lerpFrom;
	private Vector3 lerpTo;
	private float lerpProgress;
	private bool lerping;


	public override void OnEnable()
	{
		base.OnEnable();
		playersLayer = LayerMask.NameToLayer("Players");
		npcLayer = LayerMask.NameToLayer("NPC");
		windowsLayer = LayerMask.NameToLayer("Windows");
		checkMask = LayerMask.GetMask("Players", "NPC", "Windows");
	}

	protected override void OnPushSolid(Vector3Int destination)
	{
		CheckForAttackTarget();
	}

	protected override void OnTileReached(Vector3Int tilePos)
	{
		base.OnTileReached(tilePos);
		CheckForAttackTarget();
	}

	//Where is the target? Is there something in the way we can break
	//to get to the target?
	private bool CheckForAttackTarget()
	{
		if (followTarget != null)
		{
			var dirToTarget = (followTarget.position - transform.position).normalized;
			RaycastHit2D hitInfo = Physics2D.Linecast(transform.position + dirToTarget, followTarget.position, checkMask);
			Debug.DrawLine(transform.position + dirToTarget, followTarget.position, Color.blue, 10f);
			if (hitInfo.collider != null)
			{
				if (Vector3.Distance(transform.position, hitInfo.point) < 1.5f)
				{
					var dir = ((Vector3)hitInfo.point - transform.position).normalized;

					//What to do with player hit?
					if (hitInfo.transform.gameObject.layer == playersLayer)
					{
						AttackFlesh(dir,hitInfo.transform.GetComponent<Meleeable>());

						if (followTarget.gameObject.layer == playersLayer)
						{
							if (followTarget != hitInfo.transform)
							{
								if (targetOtherPlayersWhoGetInWay)
								{
									followTarget = hitInfo.transform;
								}
							}
						}

						return true;
					}

					//What to do with NPC hit?
					if (hitInfo.transform.gameObject.layer == npcLayer)
					{
						var meleeable = hitInfo.transform.GetComponent<Meleeable>();
						if (meleeable != null)
						{
							AttackFlesh(dir, meleeable);
							return true;
						}
					}

					//What to do with Window hits?
					if (hitInfo.transform.gameObject.layer == windowsLayer)
					{
						var interactableTile = hitInfo.transform.GetComponent<InteractableTiles>();
						if (interactableTile != null)
						{
							AttackTile(dir, interactableTile);
							return true;
						}
					}
				}
			}
		}

		return false;
	}

	private void AttackFlesh(Vector2 dir, Meleeable meleeable)
	{
		ServerDoLerpAnimation(dir);
	}

	private void AttackTile(Vector2 dir, InteractableTiles interactableTiles)
	{
		ServerDoLerpAnimation(dir);
	}

	private void ServerDoLerpAnimation(Vector2 dir)
	{
		var angleOfDir = Vector3.Angle(dir, transform.up);
		if (dir.x < 0f)
		{
			angleOfDir = -angleOfDir;
		}
		dirSprites.CheckSpriteServer(angleOfDir);

		Pause = true;
		StartCoroutine(WaitToUnPause(1f));
		MobMeleeLerpMessage.Send(gameObject, dir);
	}

	IEnumerator WaitToUnPause(float timeToWait)
	{
		yield return WaitFor.Seconds(timeToWait);

		if (Random.value > 0.2f) //80% chance of hitting the target again
		{
			if (!CheckForAttackTarget())
			{
				Pause = false;
			}
		}
		else
		{
			Pause = false;
		}
	}

	public void ClientDoLerpAnimation(Vector2 dir)
	{
		lerpFrom = spriteHolder.transform.localPosition;
		lerpTo = spriteHolder.transform.localPosition + (Vector3)(dir * 0.5f);

		lerpProgress = 0f;
		isForLerpBack = true;
		lerping = true;
	}

	private void ResetLerp()
	{
		lerpProgress = 0f;
		lerping = false;
		isForLerpBack = false;
	}

	protected override void UpdateMe()
	{
		CheckLerping();
		base.UpdateMe();
	}

	void CheckLerping()
	{
		if (lerping)
		{
			lerpProgress += Time.deltaTime;
			spriteHolder.transform.localPosition = Vector3.Lerp(lerpFrom, lerpTo, lerpProgress * 7f);
			if (spriteHolder.transform.localPosition == lerpTo || lerpProgress > 2f)
			{
				if (!isForLerpBack)
				{
					ResetLerp();
					spriteHolder.transform.localPosition = Vector3.zero;
				}
				else
				{
					//To lerp back
					ResetLerp();
					lerpTo = lerpFrom;
					lerpFrom = spriteHolder.transform.localPosition;
					lerping = true;
				}
			}
		}
	}
}