using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;

public class SpriteUpdateMessage : ServerMessage
{
	public static List<char> ControlCharacters = new List<char>()
	{
		'>', '<', '&', ',', '?', '~', '`', '@', '{', '£'
	};

	public string SerialiseData;


	//> = PresentSpriteSet
	//< = VariantIndex
	//& = CataloguePage
	//, = PushTexture
	//? = Empty
	//~ = PushClear
	//` = SetColour
	public static StringBuilder ToReturn = new StringBuilder("", 1000);

	public override void Process()
	{
		if (CustomNetworkManager.Instance._isServer == true) return;
		if (SerialiseData != "")
		{
			Logger.Log("yo > " + SerialiseData);
			var Cool = SerialiseData.Split('£');
			foreach (var ST in Cool)
			{
				if (ST == "")
				{
					continue;
				}

				Logger.Log("ST > " + ST);
				int Start = 0;
				int Scanning = GoToIndexOfCharacter(ST, '@', Start);

				uint NetID = uint.Parse(ST.Substring(Start, Scanning - Start));
				if (!NetworkIdentity.spawned.ContainsKey(NetID) || NetworkIdentity.spawned[NetID] == null) continue;

				Start = Scanning + 1;
				Scanning = GoToIndexOfCharacter(ST, '{', Scanning);
				string Name = ST.Substring(Start, Scanning - Start);

				var SP = SpriteHandlerManager.Instance.PresentSprites[NetworkIdentity.spawned[NetID]][Name];

				Scanning++;

				if (ST.Length > Scanning  && ST[Scanning] == '>')
				{
					Scanning += 1;
					Start = Scanning;
					Scanning = GotoIndexOfNextControlCharacter(ST, Scanning);
					int SpriteID = int.Parse(ST.Substring(Start, Scanning - Start));
					SP.SetSpriteSO(SpriteCatalogue.Instance.Catalogue[SpriteID], Network : false);
				}

				if (ST.Length > Scanning && ST[Scanning ] == '<')
				{
					Scanning += 1;
					Start = Scanning;
					Scanning = GotoIndexOfNextControlCharacter(ST, Scanning);

					SP.ChangeSpriteVariant(int.Parse(ST.Substring(Start, Scanning - Start)), NetWork: false);
				}

				if (ST.Length > Scanning  && ST[Scanning] == '&')
				{
					Scanning += 1;
					Start = Scanning;
					Scanning = GotoIndexOfNextControlCharacter(ST, Scanning);
					SP.ChangeSprite(int.Parse(ST.Substring(Start, Scanning - Start)), false);
				}

				if (ST.Length > Scanning && ST[Scanning] == ',')
				{
					Scanning++;
					SP.PushTexture();
				}

				if (ST.Length > Scanning && ST[Scanning] == '?')
				{
					Scanning++;
					SP.Empty();
					Logger.Log("Empty");
				}


				if (ST.Length > Scanning  && ST[Scanning ] == '~')
				{
					Scanning++;
					SP.PushClear();
					Logger.Log("PushClear");
				}

				if (ST.Length > Scanning && ST[Scanning ] == '`')
				{
					Color TheColour = Color.white;
					ColorUtility.TryParseHtmlString(ST.Substring(Start + 1, 8), out TheColour);
					SP.SetColor(TheColour, false);
				}

			}
		}
	}

	public static int GoToIndexOfCharacter(string SearchingIn, char Character, int CurrentLocation)
	{
		while (SearchingIn[CurrentLocation] != Character)
		{
			var carr = SearchingIn[CurrentLocation];
			CurrentLocation++;
		}

		var cDarr = SearchingIn[CurrentLocation];
		return CurrentLocation;
	}

	public static int GotoIndexOfNextControlCharacter(string SearchingIn, int CurrentLocation)
	{
		while (SearchingIn.Length > CurrentLocation &&
		       ControlCharacters.Contains(SearchingIn[CurrentLocation]) == false)
		{
			CurrentLocation++;
		}

		return CurrentLocation;
	}

	public static SpriteUpdateMessage SendToAll(Dictionary<SpriteHandler, SpriteHandlerManager.SpriteChange> ToSend)
	{
		SpriteUpdateMessage msg = new SpriteUpdateMessage();
		GenerateStates(msg, ToSend);
		ToReturn.Clear();
		msg.SendToAll();

		return msg;
	}

	public static void GenerateStates(SpriteUpdateMessage spriteUpdateMessage,
		Dictionary<SpriteHandler, SpriteHandlerManager.SpriteChange> ToSend)
	{
		foreach (var VARIABLE in ToSend)

		{
			ToReturn.Append(VARIABLE.Key.GetMasterNetID().netId.ToString());
			ToReturn.Append("@");
			ToReturn.Append(VARIABLE.Key.name);
			ToReturn.Append("{");
			GenerateSerialisation(VARIABLE.Value);
		}

		spriteUpdateMessage.SerialiseData = ToReturn.ToString();
		if (spriteUpdateMessage.SerialiseData != "")
		{
			Logger.Log(spriteUpdateMessage.SerialiseData);
		}
	}


	public static void GenerateSerialisation(SpriteHandlerManager.SpriteChange spriteChange)
	{
		if (spriteChange.PresentSpriteSet != -1)
		{
			ToReturn.Append(">");
			ToReturn.Append(spriteChange.PresentSpriteSet.ToString());
		}

		if (spriteChange.VariantIndex != -1)
		{
			ToReturn.Append("<");
			ToReturn.Append(spriteChange.VariantIndex.ToString());
		}

		if (spriteChange.CataloguePage != -1)
		{
			ToReturn.Append("&");
			ToReturn.Append(spriteChange.CataloguePage.ToString());
		}

		if (spriteChange.PushTexture)
		{
			ToReturn.Append(",");
		}

		if (spriteChange.Empty)
		{
			ToReturn.Append("?");
		}

		if (spriteChange.PushClear)
		{
			ToReturn.Append("~");
		}

		if (spriteChange.SetColour != null)
		{
			ToReturn.Append("`");
			ToReturn.Append(ColorUtility.ToHtmlStringRGBA(spriteChange.SetColour.GetValueOrDefault(Color.white)));
			//ColorUtility.TryParseHtmlString
		}

		ToReturn.Append("£");
	}
}