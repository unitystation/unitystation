using UnityEngine;
using Mirror;

//TODO: The actual sprite handling needs to be offloaded to SpriteHandler at some point
//SpriteHandler needs some clean up before that can happen

/// <summary>
/// Easy to use Directional sprite handler for NPCs
/// </summary>
public class NPCDirectionalSprites : NetworkBehaviour
{
	private LivingHealthBehaviour health;
	private UprightSprites uprightSprites;
	public SpriteRenderer spriteRend;
	public Sprite upSprite;
	public Sprite rightSprite;
	public Sprite downSprite;
	public Sprite leftSprite;

	private Vector2 localPosCache;

	/// <summary>
	/// Gets the current facing direction of the NPC
	/// </summary>
	public Vector2 CurrentFacingDirection
	{
		get { return GetDirection(dir); }
	}

	[SyncVar(hook=nameof(SyncDirChange))] private int dir;
	[SyncVar(hook=nameof(SyncRotChange))] private float spriteRot;

	void OnEnable()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (health != null) return;
		health = GetComponent<LivingHealthBehaviour>();
		uprightSprites = GetComponent<UprightSprites>();
	}

	public override void OnStartServer()
	{
		EnsureInit();
		localPosCache = transform.localPosition;
		dir = 2;
		spriteRot = 0;
	}

	public override void OnStartClient()
	{
		EnsureInit();
		SyncDirChange(dir, dir);
		SyncRotChange(spriteRot, spriteRot);
	}

	//0=no init ,1=up ,2=right ,3=down ,4=left
	void SyncDirChange(int oldDirection, int direction)
	{
		EnsureInit();
		dir = direction;
		ChangeSprite(direction);
	}

	//The local rotation of the sprite obj
	void SyncRotChange(float oldRot, float newRot)
	{
		EnsureInit();
		spriteRot = newRot;
		spriteRend.transform.localEulerAngles = new Vector3(0f,0f, spriteRot);
	}

	/// <summary>
	/// Sets the local rotation of the sprite obj
	/// </summary>
	/// <param name="newRot"></param>
	public void SetRotationServer(float newRot)
	{
		spriteRot = newRot;
	}

	/// <summary>
	/// Use this method to update the direction sprite based on an angle
	/// in degrees (-180f to 180f);
	/// </summary>
	/// <param name="angleDirection"></param>
	public void CheckSpriteServer(float angleDirection)
	{
		if (health.IsDead || health.IsCrit) return;

		if (uprightSprites != null)
		{
			var newAngle = angleDirection + (uprightSprites.ExtraRotation.eulerAngles.z * -1f);
			if (newAngle > 180f)
			{
				newAngle = -180 + (newAngle - 180f);
			}

			if (newAngle < -180f)
			{
				newAngle = 180f + (newAngle + 180f);
			}

			angleDirection = newAngle;
		}

		var tryGetDir = GetDirNumber(angleDirection);
		ChangeSprite(tryGetDir);
		dir = tryGetDir;
	}

	/// <summary>
	/// For setting the sprite dir num manually
	/// server side
	/// </summary>
	public void DoManualChange(int spriteNum)
	{
		ChangeSprite(spriteNum);
		dir = spriteNum;
	}

	/// <summary>
	/// Gets the directional number used in the syncvar based
	/// on an angle. Angle should be in -Pi, Pi values (-180, 180)
	/// </summary>
	/// 1=up ,2=right ,3=down ,4=left
	private int GetDirNumber(float angle)
	{
		if (angle == 0f)
		{
			return 1;
		}

		if (angle == -180f || angle == 180f)
		{
			return 3;
		}

		if (angle > 0f)
		{
			return 2;
		}

		if (angle < 0f)
		{
			return 4;
		}

		return 2;
	}

	private Vector2 GetDirection(int dirNum)
	{
		switch (dirNum)
		{
			case 1:
				return Vector2.up;
			case 2:
				return Vector2.right;
			case 3:
				return Vector2.down;
			case 4:
				return Vector2.left;
			default:
				return Vector2.left;
		}
	}

	/// 1=up ,2=right ,3=down ,4=left
	private void ChangeSprite(int dirNum)
	{
		switch (dirNum)
		{
			case 1:
				spriteRend.sprite = upSprite;
				break;
			case 2:
				spriteRend.sprite = rightSprite;
				break;
			case 3:
				spriteRend.sprite = downSprite;
				break;
			case 4:
				spriteRend.sprite = leftSprite;
				break;
		}
	}

	/// <summary>
	/// Set the sprite renderer to bodies when the mob has died
	/// </summary>
	public void SetToBodyLayer()
	{
		spriteRend.sortingLayerName = "Bodies";
	}

	/// <summary>
	/// Set the mobs sprite renderer to NPC layer
	/// </summary>
	public void SetToNPCLayer()
	{
		spriteRend.sortingLayerName = "NPCs";
	}

	/// <summary>
	/// Change the facing direction based on a Vector2 dir
	/// </summary>
	/// <param name="dir"></param>
	public void ChangeDirection(Vector2 dir)
	{
		var angleOfDir = Vector3.Angle((Vector2) dir, transform.up);
		if (dir.x < 0f)
		{
			angleOfDir = -angleOfDir;
		}

		CheckSpriteServer(angleOfDir);
	}
}