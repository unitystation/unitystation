using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;

#endif

/// <summary>
/// Stores and Process SpriteData
/// To be used in SpriteHandler
/// </summary>
public class SpriteDataHandler : MonoBehaviour
{
	//TODO
	//Maybe a dictionary so you can easily look up in hands and stuff like that
	//With enum
	[FormerlySerializedAs("SpriteInfos")]
	[Tooltip("Do not edit this by hand! Use set up sheet!")]
	public SpriteData Infos;

	// List of sprites to be used. If the sprite has an animation, only the zeroth is needed.
	// Left hand and right hand sprites need to be first in the list.
	public List<Sprite> spriteList = new List<Sprite>();

	private SpriteJson spriteJson;

	public void Awake()
	{
		Infos.DeSerializeT();
	}

	public virtual void OnEnable()
	{
	}

#if UNITY_EDITOR
	public virtual void SetUpSheet()
	{
		Infos = new SpriteData();
		Infos.List = new List<List<List<SpriteInfo>>>();
		for (int i = 0; i < spriteList.Count; i++)
		{
			Infos.List.Add(new List<List<SpriteInfo>>());
			var path = AssetDatabase.GetAssetPath(spriteList[i])
				.Substring(17); //Substring(17) To remove the "Assets/Resources/"
			Sprite[] spriteSheetSprites = Resources.LoadAll<Sprite>(path.Remove(path.Length - 4));
			if (spriteSheetSprites.Length > 1)
			{
				var p = AssetDatabase.GetAssetPath(spriteList[i]);
				int extensionIndex = p.LastIndexOf(".");
				p = p.Substring(0, extensionIndex) + ".json";
				string json = System.IO.File.ReadAllText(p);
				Logger.Log(json.ToString());
				spriteJson = JsonConvert.DeserializeObject<SpriteJson>(json);

				int c = 0;
				int cone = 0;
				for (int J = 0; J < spriteJson.Number_Of_Variants; J++)
				{
					Infos.List[i].Add(new List<SpriteInfo>());
				}

				foreach (var SP in spriteSheetSprites)
				{
					var info = new SpriteInfo();
					info.sprite = SP;
					if (spriteJson.Delays.Count > 0)
					{
						info.waitTime = spriteJson.Delays[c][cone];
					}

					Infos.List[i][c].Add(info);

					if (c >= (spriteJson.Number_Of_Variants - 1))
					{
						c = 0;
						cone++;
					}
					else
					{
						c++;
					}
				}
			}
			else
			{
				var info = new SpriteInfo()
				{
					sprite = spriteSheetSprites[0],
					waitTime = 0
				};
				Infos.List[i].Add(new List<SpriteInfo>());
				Infos.List[i][0].Add(info);
			}
		}

		Infos.SerializeT();
		var IA = this.GetComponent<ItemAttributes>();
		if (IA != null)
		{
			IA.spriteDataHandler = this;
		}

		var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
		if (prefabStage != null)
		{
			EditorSceneManager.MarkSceneDirty(prefabStage.scene);
			Logger.Log("Setup Complete", Category.SpriteHandler);
		}
	}
#endif

	public class SpriteInfo
	{
		public Sprite sprite;
		public float waitTime;
	}

	public class SpriteJson
	{
		public List<List<float>> Delays;
		public int Number_Of_Variants;
		public int Frames_Of_Animation;
	}
}