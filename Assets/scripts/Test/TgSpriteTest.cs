using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;


	public class TgSpriteTest {

		[MenuItem("Tools/Test DMI")]
		private static void Awake() {
			Dictionary<string, DmiIcon> data = new Dictionary<string, DmiIcon>();

			IconList<DmiIcon> ilist = DeserializeJson();
			Debug.Log("ilist.size = " + ilist.icons.Count);
			foreach (var icon in ilist.icons)
			{
				var substring = icon.icon.Substring(0, icon.icon.IndexOf(".dmi", StringComparison.Ordinal));
				Sprite[] sprites = Resources.LoadAll<Sprite>(
					substring
				); //todo: consider excluding extensions on java side to avoid substr mess?
			
				icon.spriteSheet = sprites;
				data.Add(substring.Substring("icons/".Length), icon);
			}
			Debug.Log("data.size = " + data.Count);
			StringBuilder sb = new StringBuilder();
			foreach (var key in data.Keys)
			{
				sb.AppendLine(key + ": " + data[key]);
			}
			Debug.Log(sb.ToString());
			Debug.Log(data["mob/human"]);
		}
		private static IconList<DmiIcon> DeserializeJson()
		{
			string myJson = null;
			var asset = Resources.Load(Path.Combine("metadata", "dmi")) as TextAsset;
			if (asset != null)
			{
				//workaround for headerless JSONs
				myJson = "{ \"icons\": " + asset.text + "}";
			} else Debug.LogError("Make sure dmi.json is in Resources/metadata/ !");
			
			var icons = new IconList<DmiIcon>();
			JsonUtility.FromJsonOverwrite(myJson, icons);
			return icons;

		}
		[Serializable]
		private class IconList<T>
		{
			public List<T> icons;
		}
		//[MenuItem("Tools/Test minishit")]
		public static void DeserializeJsonMini()
		{
			TextAsset asset = Resources.Load(Path.Combine("metadata", "dmi1")) as TextAsset;

			DmiIcon icon = new DmiIcon();
			JsonUtility.FromJsonOverwrite(asset.text, icon);
				Debug.Log(icon);
		}

	}