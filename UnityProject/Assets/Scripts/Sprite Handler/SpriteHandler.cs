using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;


///	<summary>
///	Handles sprite syncing between server and clients and contains a custom animator
///	</summary>
public class SpriteHandler : NetworkBehaviour
{
	public SpriteRenderer spriteRenderer;

	public List<SpriteList> spriteList = new List<SpriteList>();
	[SyncVar(hook = nameof(SyncSprite))] public int spriteIndex;

	private List<SpriteInfo> infoList = new List<SpriteInfo>();
	private SpriteJson spriteJson;
	private int animationIndex = 0;
	private float timeElapsed = 0;
	private float waitTime;

	private void Awake() {
		infoList.Add(new SpriteInfo());
	}

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
			if(animationIndex >= spriteJson.Frames_Of_Animation)
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
		//infoList = new List<SpriteInfo>();
		if (newSprites.Count > 1)
		{
			LoadJson(AssetDatabase.GetAssetPath(newSprites[0]));

			while (newSprites.Count > infoList.Count)
			{
				infoList.Add(new SpriteInfo());
			}
			for (int i = 0; i < newSprites.Count; i++)
			{
				infoList[i].sprite = newSprites[i];
				infoList[i].waitTime = spriteJson.Delays[i];
			}

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

	void LoadJson(string path)
	{
		int extensionIndex = path.LastIndexOf(".");
		path = path.Substring(0, extensionIndex) + ".json";
		string json = System.IO.File.ReadAllText(path);
		spriteJson = JsonUtility.FromJson<SpriteJson>(json);
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
	}

	class SpriteJson
	{
		public float[] Delays;
		public int Number_Of_Variants;
		public int Frames_Of_Animation;
	}
}

