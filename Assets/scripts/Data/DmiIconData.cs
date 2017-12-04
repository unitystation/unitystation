using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "DmiIconData")]
public class DmiIconData : ScriptableObject
{

    private static Dictionary<string, DmiIcon> data = new Dictionary<string, DmiIcon>();
    private static Dictionary<string, DmiIcon> legacyData = new Dictionary<string, DmiIcon>();

    //	public static Dictionary<string, DmiIcon> LegacyData => legacyData;
    public static Dictionary<string, DmiIcon> Data => data;

    //Stuff for SpriteManager compatibility
    public Sprite[] getSprites(string path)
    {
        var iPath = getIconPath(path);
        //		var iconPath = "icons/" + path + ".dmi";
        if (data.ContainsKey(iPath))
        {
            var sprites = data[iPath].spriteSheet;
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
        var iPath = getIconPath(spriteSheet);
        if (legacyData.ContainsKey(iPath))
        {
            var icon = legacyData[iPath];
            var legacyOffset = DmiIcon.getOffsetFromUnityName(legacyUnityName);
            int relativeOffset;
            var legacyDmiState = icon.getStateAtOffset(legacyOffset, out relativeOffset);
            if (legacyDmiState != null && data.ContainsKey(iPath))
            {
                var legacyState = legacyDmiState.state;
                var newState = data[iPath].getState(legacyState);//searchStateInIcon(legacyState, spriteSheet, false);
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
        var icon = getIconBySheet(spriteSheet);
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
        var uOffset = DmiIcon.getOffsetFromUnityName(unityName);
        if (!uOffset.Equals(-1))
        {
            return getSprite(spriteSheet, uOffset);
        }
        //if it's something custom ,like tileconnect handwritten stuff
        var icon = getIconBySheet(spriteSheet);
        if (!icon.getName().Equals(""))
        {
            var dmiState = icon.states.Find(state => state.unityName.Equals(unityName));
            if (dmiState != null)
            {
                var offset = dmiState.offset;
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
        var iPath = getIconPath(path);
        if (data.ContainsKey(iPath))
        {
            var icon = data[iPath];
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
        var tmpData = data.Where(p => p.Key.StartsWith(scanPath))
            .ToDictionary(p => p.Key, p => p.Value);
        foreach (var dmiIcon in tmpData.Values)
        {

            var foundState = dmiIcon.states.Find(x => x.state == state);
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
        foreach (var dmiIcon in inLegacy ? legacyData.Values : data.Values)
        {
            var foundState = dmiIcon.states.Find(x => x.state.Equals(state));
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
            foreach (var dmiIcon in data.Values)
            {
                var foundState = dmiIcon.states.Find(x => x.state.Equals(state) && (dirs == -1 || x.dirs == dirs));
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
        return searchStateInIcon(state, new[] { icon }, deepSearch);
    }

    public DmiState searchStateFourDirectional(string state, string icon)
    {
        return searchStateInIcon(state, new[] { icon }, 4, true);
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

                if (data.ContainsKey(s))
                {
                    var icon = data[s]; /*data.Values.ToList().Find(x => x.icon == s);*/

                    var foundState = icon.states.Find(
                        x => (x.state == state) && (dirs == -1 || x.dirs == dirs)
                    );
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
        if (data.Count != 0) return;
        IconList<DmiIcon> ilist = DeserializeJson("dmi");
        IconList<DmiIcon> iLegacylist = DeserializeJson("legacydmi");
        //		KeyValuePair<IconList<DmiIcon>, Dictionary<string, DmiIcon>> listsKeyValuePair
        var lists = new Dictionary<IconList<DmiIcon>, Dictionary<string, DmiIcon>>();
        lists.Add(ilist, data);
        lists.Add(iLegacylist, legacyData);
        //		{iLegacylist, legacyData}};
        foreach (var list in lists)
        {
            foreach (var icon in list.Key.icons)
            {
                var substring = icon.icon.Substring(0, icon.icon.IndexOf(".dmi", StringComparison.Ordinal));
                Sprite[] sprites = Resources.LoadAll<Sprite>(
                    substring
                ); //todo: consider cutting off 'icons/' and extension on java side to avoid further substr mess?

                icon.spriteSheet = sprites;
                list.Value.Add(icon.icon, icon);
            }
        }

    }

    private static IconList<DmiIcon> DeserializeJson(string name)
    {
        string myJson = null;
        var asset = Resources.Load(Path.Combine("metadata", name)) as TextAsset;
        if (asset != null)
        {
            //workaround for headerless JSONs
            myJson = "{ \"icons\": " + asset.text + "}";
        }
        else Debug.LogError("Make sure dmi.json is in Resources/metadata/ !");

        var icons = new IconList<DmiIcon>();
        JsonUtility.FromJsonOverwrite(myJson, icons);
        return icons;

    }
    [Serializable]
    private class IconList<T>
    {
        public List<T> icons;
    }
}
