using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SpriteHandler : NetworkBehaviour
{
	public SpriteRenderer spriteRenderer;

	public List<SpriteList> spriteList = new List<SpriteList>();
	[SyncVar(hook = nameof(SyncSprite))] public int spriteIndex;

	private List<SpriteInfo> infoList = new List<SpriteInfo>();
	private int animationIndex = 0;
	private float timeElapsed = 0;
	private float waitTime;

	public override void OnStartClient()
	{
		SyncSprite(spriteIndex);
	}

	public void UpdateMe()
	{
		timeElapsed += Time.deltaTime;
		if(timeElapsed >= waitTime)
		{
			animationIndex++;
			if(animationIndex >= infoList.Count)
			{
				animationIndex = 0;
			}
			SetSprite(infoList[animationIndex]);
		}
	}

	void SetSprite(SpriteInfo animationStills)
	{
		timeElapsed = 0;
		waitTime = animationStills.waitTime;
		spriteRenderer.sprite = animationStills.sprite;
	}

	void SetSpriteList(List<Sprite> newSprites)
	{
		infoList = new List<SpriteInfo>();
		for (int i = 0; i < newSprites.Count; i++)
		{
			var newSprite = new SpriteInfo(newSprites[i], 0.1f);
			infoList.Add(newSprite);
		}
		if(newSprites.Count > 1)
		{
			UpdateManager.Instance.Add(UpdateMe);
		}
		else
		{
			UpdateManager.Instance.Remove(UpdateMe);
		}
		SetSprite(infoList[0]);
	}

	public void ChangeSprite(int newSprites)
	{
		if(spriteIndex != newSprites)
		{
			spriteIndex = newSprites;
		}
	}

	void SyncSprite(int value)
	{
		SetSpriteList(spriteList[value].spriteList);
	}

	[System.Serializable]
	public class SpriteList
	{
		public List<Sprite> spriteList = new List<Sprite>();
	}

	class SpriteInfo
	{
		public Sprite sprite;
		public float waitTime;

		public SpriteInfo(Sprite newSprite, float newTime)
		{
			sprite = newSprite;
			waitTime = newTime;
		}
	}
}

