using System.Collections.Generic;
using Shared.Managers;
using TMPro;
using UnityEngine;

public class ChatManager : SingletonManager<ChatManager>
{
	[field: SerializeField] public List<TMP_FontAsset> Fonts = new List<TMP_FontAsset>();
	public string FontIndexToUse = "LiberationSans SDF";
}
