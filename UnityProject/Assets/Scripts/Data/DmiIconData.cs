using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "DmiIconData")]
public class DmiIconData : ScriptableObject
{
	private static readonly Dictionary<string, DmiIcon> legacyData = new Dictionary<string, DmiIcon>();

	//	public static Dictionary<string, DmiIcon> LegacyData => legacyData;
	public static Dictionary<string, DmiIcon> Data { get; } = new Dictionary<string, DmiIcon>();

	//Stuff for SpriteManager compatibility
	public Sprite[] getSprites(string path)
	{
		string iPath = getIconPath(path);
		//		var iconPath = "icons/" + path + ".dmi";
		if (Data.ContainsKey(iPath))
		{
			Sprite[] sprites = Data[iPath].spriteSheet;
			if (sprites != null)
			{
				return sprites;
			}
		}
		Debug.LogError("Could not find sprites for key " + path);
		return new Sprite[0];
	}

	public Sprite getSpriteFromLegacyName(string spriteSheet, string legacyUnityName)
	{
		string iPath = getIconPath(spriteSheet);
		if (legacyData.ContainsKey(iPath))
		{
			DmiIcon icon = legacyData[iPath];
			int legacyOffset = DmiIcon.getOffsetFromUnityName(legacyUnityName);
			int relativeOffset;
			DmiState legacyDmiState = icon.getStateAtOffset(legacyOffset, out relativeOffset);
			if (legacyDmiState != null && Data.ContainsKey(iPath))
			{
				string legacyState = legacyDmiState.state;
				DmiState newState = Data[iPath].getState(legacyState); //searchStateInIcon(legacyState, spriteSheet, false);
				if (newState != null)
				{
					//					if (legacyUnityName.Contains("shuttle_wall"))
					//					{
					//						Debug.Log("found ya!");
					//					}
					return getSprite(spriteSheet, newState.offset + relativeOffset);
				}
			}
		}
		Debug.LogErrorFormat("failed to getSpriteFromLegacyName: {0} {1}", spriteSheet, legacyUnityName);
		return new Sprite();
	}

	public Sprite getSprite(string spriteSheet, int offset)
	{
		DmiIcon icon = getIconBySheet(spriteSheet);
		if (!icon.getName().Equals("") && offset >= 0 && offset < icon.spriteSheet.Length)
		{
			return icon.spriteSheet[offset];
		}
		Debug.LogErrorFormat("Couldn't find sprite by offset: {0}({1}) in {2}", spriteSheet, offset, icon.icon);
		return new Sprite();
	}

	public Sprite getSprite(string spriteSheet, string unityName)
	{
		//if it's a proper unityName with offset
		int uOffset = DmiIcon.getOffsetFromUnityName(unityName);
		if (!uOffset.Equals(-1))
		{
			return getSprite(spriteSheet, uOffset);
		}
		//if it's something custom ,like tileconnect handwritten stuff
		DmiIcon icon = getIconBySheet(spriteSheet);
		if (!icon.getName().Equals(""))
		{
			DmiState dmiState = icon.states.Find(state => state.unityName.Equals(unityName));
			if (dmiState != null)
			{
				int offset = dmiState.offset;
				if (icon.spriteSheet.Length > offset && !offset.Equals(-1))
				{
					return icon.spriteSheet[offset];
				}
			}
			Debug.LogErrorFormat("Couldn't find sprite by UN: {0}({1}) in {2}", spriteSheet, unityName, icon.icon);
		}
		return new Sprite();
	}

	public DmiIcon getIconBySheet(string path)
	{
		string iPath = getIconPath(path);
		if (Data.ContainsKey(iPath))
		{
			DmiIcon icon = Data[iPath];
			if (icon != null)
			{
				return icon;
			}
		}
		Debug.LogError("Could not find Icon for sheet " + iPath);
		return new DmiIcon();
	}

	public DmiIcon getIconByState(string state, string scanPath)
	{
		Dictionary<string, DmiIcon> tmpData = Data.Where(p => p.Key.StartsWith(scanPath)).ToDictionary(p => p.Key, p => p.Value);
		foreach (DmiIcon dmiIcon in tmpData.Values)
		{
			DmiState foundState = dmiIcon.states.Find(x => x.state == state);
			if (foundState != null)
			{
				//				Debug.Log("foundState: "+ foundState);
				return dmiIcon;
			}
		}
		//		Debug.Log("Couldn't find dmiIcon by state " + state + " in " + scanPath + ", deepScanning!");
		return getIconByState(state);
	}

	public DmiIcon getIconByState(string state, bool inLegacy = false)
	{
		foreach (DmiIcon dmiIcon in inLegacy ? legacyData.Values : Data.Values)
		{
			DmiState foundState = dmiIcon.states.Find(x => x.state.Equals(state));
			if (foundState != null)
			{
				//				Debug.Log("foundState: "+ foundState);
				return dmiIcon;
			}
		}
		Debug.LogWarning("Couldn't find dmiIcon by state " + state);
		return null;
	}

	public DmiState searchState(string state)
	{
		return searchState(state, -1);
	}

	public DmiState searchState(string state, int dirs)
	{
		if (state != "")
		{
			foreach (DmiIcon dmiIcon in Data.Values)
			{
				DmiState foundState = dmiIcon.states.Find(x => x.state.Equals(state) && (dirs == -1 || x.dirs == dirs));
				if (foundState != null)
				{
					//				Debug.Log("foundState: "+ foundState);
					return foundState;
				}
			}
		}
		Debug.LogWarning("Couldn't find state " + state + " in the entire datafile!");
		return null;
	}


	public DmiState searchStateInIconShallow(string state, string icon)
	{
		return searchStateInIcon(state, icon, false);
	}

	public DmiState searchStateInIcon(string state, string icon, bool deepSearch)
	{
		return searchStateInIcon(state, new[] {icon}, deepSearch);
	}

	public DmiState searchStateFourDirectional(string state, string icon)
	{
		return searchStateInIcon(state, new[] {icon}, 4, true);
	}

	public DmiState searchStateFourDirectional(string state, string[] icons)
	{
		return searchStateInIcon(state, icons, 4, true);
	}

	public DmiState searchStateInIcon(string state, string[] icons, bool deepSearch)
	{
		return searchStateInIcon(state, icons, -1, deepSearch);
	}

	public DmiState searchStateInIcon(string state, string[] icons, int dirs, bool deepSearch)
	{
		for (int i = 0; i < icons.Length; i++)
		{
			if (icons[i] != "" && state != "")
			{
				string s = getIconPath(icons[i]);

				if (Data.ContainsKey(s))
				{
					DmiIcon icon = Data[s]; /*data.Values.ToList().Find(x => x.icon == s);*/

					DmiState foundState = icon.states.Find(x => x.state == state && (dirs == -1 || x.dirs == dirs));
					if (foundState != null)
					{
						//						Debug.Log("foundState: "+ foundState);
						return foundState;
					}
				}
			}
		}
		if (deepSearch)
		{
			//			Debug.Log("Could not find " + state + " in " + icons + ". Deepscanning!");
			return searchState(state, dirs);
		}
		//		Debug.Log("Couldn't find state " + state + " using shallowSearch");
		return null;
	}

	public static string getIconPath(string s)
	{
		if (!s.Contains("."))
		{
			s = s + ".dmi";
		}
		if (!s.StartsWith("icons/"))
		{
			s = "icons/" + s;
		}
		return s;
	}

	private void OnEnable()
	{
		if (Data.Count != 0)
		{
			return;
		}
		IconList<DmiIcon> ilist = DeserializeJson("dmi");
		IconList<DmiIcon> iLegacylist = DeserializeJson("legacydmi");
		//		KeyValuePair<IconList<DmiIcon>, Dictionary<string, DmiIcon>> listsKeyValuePair
		Dictionary<IconList<DmiIcon>, Dictionary<string, DmiIcon>> lists = new Dictionary<IconList<DmiIcon>, Dictionary<string, DmiIcon>>();
		lists.Add(ilist, Data);
		lists.Add(iLegacylist, legacyData);
		//		{iLegacylist, legacyData}};
		foreach (KeyValuePair<IconList<DmiIcon>, Dictionary<string, DmiIcon>> list in lists)
		{
			foreach (DmiIcon icon in list.Key.icons)
			{
				string substring = icon.icon.Substring(0, icon.icon.IndexOf(".dmi", StringComparison.Ordinal));
				Sprite[]
					sprites = Resources.LoadAll<Sprite>(substring); //todo: consider cutting off 'icons/' and extension on java side to avoid further substr mess?

				icon.spriteSheet = sprites;
				list.Value.Add(icon.icon, icon);
			}
		}
	}

	private static IconList<DmiIcon> DeserializeJson(string name)
	{
		string myJson = null;
		TextAsset asset = Resources.Load(Path.Combine("metadata", name)) as TextAsset;
		if (asset != null)
		{
			//workaround for headerless JSONs
			myJson = "{ \"icons\": " + asset.text + "}";
		}
		else
		{
			Debug.LogError("Make sure dmi.json is in Resources/metadata/ !");
		}

		IconList<DmiIcon> icons = new IconList<DmiIcon>();
		JsonUtility.FromJsonOverwrite(myJson, icons);
		return icons;
	}

	[Serializable]
	private class IconList<T>
	{
		public List<T> icons;
	}
}