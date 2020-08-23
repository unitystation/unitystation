using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mirror;
using UnityEngine;
using Newtonsoft.Json;
using Random = System.Random;

public class SpriteUpdateMessage : ServerMessage
{
	public static List<char> ControlCharacters = new List<char>()
	{
		'>', '<', '&', ',', '?', '~', '`', '@', '{', '%', '^', '£'
	};

	public string SerialiseData;

	//> = PresentSpriteSet
	//< = VariantIndex
	//& = CataloguePage
	//, = PushTexture
	//? = Empty
	//~ = PushClear
	//` = SetColour
	//% = Pallet
	//^ = ClearPallet

	private static StringBuilder ToReturn = new StringBuilder("", 1000);

	public override void Process()
	{
		if (CustomNetworkManager.Instance._isServer == true) return;
		if (SerialiseData != "")
		{
			int Start = 0;
			int Scanning = 0;
			while (SerialiseData.Length > Scanning)
			{
				Scanning = GoToIndexOfCharacter(SerialiseData, '@', Start);
				uint NetID = uint.Parse(SerialiseData.Substring(Start, Scanning - Start));
				if (!NetworkIdentity.spawned.ContainsKey(NetID) || NetworkIdentity.spawned[NetID] == null)
				{
					Scanning = SkipSection(Start);
					Start = Scanning;
					continue;
				}


				Start = Scanning + 1;
				Scanning = GoToIndexOfCharacter(SerialiseData, '{', Scanning);
				string Name = SerialiseData.Substring(Start, Scanning - Start);

				var SP = SpriteHandlerManager.Instance.PresentSprites[NetworkIdentity.spawned[NetID]][Name];

				Scanning++;

				if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '>')
				{
					Scanning += 1;
					Start = Scanning;
					Scanning = GotoIndexOfNextControlCharacter(SerialiseData, Scanning);
					int SpriteID = int.Parse(SerialiseData.Substring(Start, Scanning - Start));
					SP.SetSpriteSO(SpriteCatalogue.Instance.Catalogue[SpriteID], Network: false);
				}

				if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '<')
				{
					Scanning += 1;
					Start = Scanning;
					Scanning = GotoIndexOfNextControlCharacter(SerialiseData, Scanning);

					SP.ChangeSpriteVariant(int.Parse(SerialiseData.Substring(Start, Scanning - Start)), NetWork: false);
				}

				if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '&')
				{
					Scanning += 1;
					Start = Scanning;
					Scanning = GotoIndexOfNextControlCharacter(SerialiseData, Scanning);
					SP.ChangeSprite(int.Parse(SerialiseData.Substring(Start, Scanning - Start)), false);
				}

				if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == ',')
				{
					Scanning++;
					SP.PushTexture(false);
				}

				if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '?')
				{
					Scanning++;
					SP.Empty(false);
				}


				if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '~')
				{
					Scanning++;
					SP.PushClear(false);
				}

				if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '^')
				{
					Scanning++;
					SP.ClearPallet(false);
				}


				if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '`')
				{
					Color TheColour = Color.white;
					TheColour.r = (SerialiseData[Scanning + 1] / 255f);
					TheColour.g = (SerialiseData[Scanning + 2] / 255f);
					TheColour.b = (SerialiseData[Scanning + 3] / 255f);
					TheColour.a = (SerialiseData[Scanning + 4] / 255f);
					Scanning = Scanning + 4;
					SP.SetColor(TheColour, false);
					Scanning++;
				}

				if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '%')
				{
					List<Color> Colours = new List<Color>();
					for (int i = 0; i < 8; i++)
					{
						Colours.Add(GetColourFromStringIndex(SerialiseData, Scanning + (i * 4)));
					}

					Scanning = Scanning + 1;
					Scanning = Scanning + 32;
					SP.SetPaletteOfCurrentSprite(Colours, false);
				}

				Scanning++;
				Start = Scanning;
			}
		}
	}

	public int SkipSection (int Start){
		int Scanning;
		Scanning = GoToIndexOfCharacter(SerialiseData, '@', Start);
		Start = Scanning + 1;
		Scanning = GoToIndexOfCharacter(SerialiseData, '{', Scanning);
		Scanning++;

		if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '>')
		{
			Scanning += 1;
			Start = Scanning;
			Scanning = GotoIndexOfNextControlCharacter(SerialiseData, Scanning);
		}

		if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '<')
		{
			Scanning += 1;
			Start = Scanning;
			Scanning = GotoIndexOfNextControlCharacter(SerialiseData, Scanning);
		}

		if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '&')
		{
			Scanning += 1;
			Start = Scanning;
			Scanning = GotoIndexOfNextControlCharacter(SerialiseData, Scanning);
		}

		if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == ',')
		{
			Scanning++;
		}

		if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '?')
		{
			Scanning++;
		}


		if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '~')
		{
			Scanning++;
		}

		if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '^')
		{
			Scanning++;
		}


		if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '`')
		{
			Scanning = Scanning + 4;
			Scanning++;
		}

		if (SerialiseData.Length > Scanning && SerialiseData[Scanning] == '%')
		{
			Scanning = Scanning + 1;
			Scanning = Scanning + 32;
		}

		Scanning++;
		return Scanning;
	}

	/// <summary>
	/// Starts on control character/previous character
	/// </summary>
	/// <param name="SearchingIn"></param>
	/// <param name="Start"></param>
	/// <returns></returns>
	public static Color GetColourFromStringIndex(string SearchingIn, int Start)
	{
		Color TheColour = Color.white;
		TheColour.r = (SearchingIn[Start + 1] / 255f);
		TheColour.g = (SearchingIn[Start + 2] / 255f);
		TheColour.b = (SearchingIn[Start + 3] / 255f);
		TheColour.a = (SearchingIn[Start + 4] / 255f);
		return TheColour;
	}

	public static int GoToIndexOfCharacter(string SearchingIn, char Character, int CurrentLocation)
	{
		while (SearchingIn[CurrentLocation] != Character)
		{
			CurrentLocation++;
		}

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

	public static void SendToSpecified(NetworkConnection recipient,
		Dictionary<SpriteHandler, SpriteHandlerManager.SpriteChange> ToSend)
	{
		foreach (var changeChunk in ToSend.Chunk(500))
		{
			var msg = GenerateMessage(changeChunk);
			msg.SendTo(recipient);
		}
	}

	public static void SendToAll(Dictionary<SpriteHandler, SpriteHandlerManager.SpriteChange> ToSend)
	{
		foreach (var changeChunk in ToSend.Chunk(500))
		{
			var msg = GenerateMessage(changeChunk);
			msg.SendToAll();
		}
	}


	public static SpriteUpdateMessage GenerateMessage(
		IEnumerable<KeyValuePair<SpriteHandler, SpriteHandlerManager.SpriteChange>> ToSend)
	{
		SpriteUpdateMessage msg = new SpriteUpdateMessage();
		GenerateStates(msg, ToSend);
		ToReturn.Clear();
		return (msg);
	}

	public static void GenerateStates(SpriteUpdateMessage spriteUpdateMessage,
		IEnumerable<KeyValuePair<SpriteHandler, SpriteHandlerManager.SpriteChange>> ToSend)
	{
		foreach (var VARIABLE in ToSend)
		{
			if(VARIABLE.Value == null) continue;

			ToReturn.Append(VARIABLE.Key.GetMasterNetID().netId.ToString());
			ToReturn.Append("@");

			if(VARIABLE.Key == null)
				ToReturn.Append("default_name");
			else
				ToReturn.Append(VARIABLE.Key.name);

			ToReturn.Append("{");
			GenerateSerialisation(VARIABLE.Value);
		}

		spriteUpdateMessage.SerialiseData = ToReturn.ToString();
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

		if (spriteChange.ClearPallet)
		{
			ToReturn.Append('^');
		}

		if (spriteChange.SetColour != null)
		{
			ToReturn.Append("`");
			ToReturn.Append(Convert.ToChar(Mathf.RoundToInt(spriteChange.SetColour.Value.r * 255)));
			ToReturn.Append(Convert.ToChar(Mathf.RoundToInt(spriteChange.SetColour.Value.g * 255)));
			ToReturn.Append(Convert.ToChar(Mathf.RoundToInt(spriteChange.SetColour.Value.b * 255)));
			ToReturn.Append(Convert.ToChar(Mathf.RoundToInt(spriteChange.SetColour.Value.a * 255)));
		}

		if (spriteChange.Pallet != null)
		{
			if (spriteChange.Pallet.Count != 8)
			{
				Logger.Log("Pallet Is not the right length has to be eight it is " + spriteChange.Pallet.Count);
				ToReturn.Append("£");
				return;
			}

			ToReturn.Append("%");

			foreach (var Colour in spriteChange.Pallet)
			{
				ToReturn.Append(Convert.ToChar(Mathf.RoundToInt(Colour.r * 255)));
				ToReturn.Append(Convert.ToChar(Mathf.RoundToInt(Colour.g * 255)));
				ToReturn.Append(Convert.ToChar(Mathf.RoundToInt(Colour.b * 255)));
				ToReturn.Append(Convert.ToChar(Mathf.RoundToInt(Colour.a * 255)));
			}
		}

		ToReturn.Append("£");
	}
}