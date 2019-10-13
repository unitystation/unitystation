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
	public SpriteRenderer spriteRend;
	public Sprite upSprite;
	public Sprite rightSprite;
	public Sprite downSprite;
	public Sprite leftSprite;

	private Vector2 localPosCache;

	[SyncVar(hook = "OnDirChange")] private int dir;

	void OnEnable()
	{
		health = GetComponent<LivingHealthBehaviour>();
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		localPosCache = transform.localPosition;
		dir = 2;
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		OnDirChange(dir);
	}

	//0=no init ,1=up ,2=right ,3=down ,4=left
	void OnDirChange(int direction)
	{
		dir = direction;
		ChangeSprite(direction);
	}

	/// <summary>
	/// Use this method to update the direction sprite based on an angle
	/// in degrees (-90f to 90f);
	/// </summary>
	/// <param name="angleDirection"></param>
	public void CheckSpriteServer(float angleDirection)
	{
		var tryGetDir = GetDirNumber(angleDirection);
		Debug.Log($"angle {angleDirection} dirNum: {tryGetDir}");
		if (tryGetDir != dir)
		{
			ChangeSprite(tryGetDir);
			dir = tryGetDir;
		}
	}

	/// <summary>
	/// Gets the directional number used in the syncvar based
	/// on an angle. Angle should be in -Pi, Pi values (-90, 90)
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
}