using System;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;

namespace Messages.Server.SpritesMessages
{
	public class SpriteUpdateMessage : ServerMessage<SpriteUpdateMessage.NetMessage>
	{
		private static readonly List<SpriteUpdateEntry> UnprocessedData = new List<SpriteUpdateEntry>();

		public struct NetMessage : NetworkMessage
		{
			public string SerialiseData;
		}

		public override void Process(NetMessage msg)
		{
			if (CustomNetworkManager.Instance._isServer)
				return;

			var spriteUpdateList = JsonUtility.FromJson<SpriteUpdateList>(msg.SerialiseData);

			spriteUpdateList.list.AddRange(UnprocessedData);
			for (var i = spriteUpdateList.list.Count - 1; i >= 0; i--)
			{
				var spriteUpdateEntry = spriteUpdateList.list[i];
				if (ProcessEntry(spriteUpdateEntry))
				{
					spriteUpdateList.list.Remove(spriteUpdateEntry);
				}
			}
			UnprocessedData.Clear();
			UnprocessedData.AddRange(spriteUpdateList.list);
		}

		private bool ProcessEntry(SpriteUpdateEntry spriteUpdateEntry)
		{
			if (NetworkIdentity.spawned.ContainsKey(spriteUpdateEntry.id) == false)
				return false;
			var networkIdentity = NetworkIdentity.spawned[spriteUpdateEntry.id];
			if (networkIdentity == null)
				return false;

			if (SpriteHandlerManager.Instance.PresentSprites.ContainsKey(networkIdentity) == false ||
			    SpriteHandlerManager.Instance.PresentSprites[networkIdentity].ContainsKey(spriteUpdateEntry.name) == false)
			{
				return false;
			}

			var spriteHandler = SpriteHandlerManager.Instance.PresentSprites[networkIdentity][spriteUpdateEntry.name];
			var argumentIndex = 0;
			foreach (var spriteOperation in spriteUpdateEntry.call)
			{
				if (spriteOperation == SpriteOperation.PresentSpriteSet)
				{
					var argument = spriteUpdateEntry.arg[argumentIndex];
					argumentIndex ++;
					spriteHandler.SetSpriteSO(SpriteCatalogue.Instance.Catalogue[argument], Network: false);
				}
				else if (spriteOperation == SpriteOperation.VariantIndex)
				{
					var argument = spriteUpdateEntry.arg[argumentIndex];
					argumentIndex ++;
					spriteHandler.ChangeSpriteVariant(argument, false);
				}
				else if (spriteOperation == SpriteOperation.CataloguePage)
				{
					var argument = spriteUpdateEntry.arg[argumentIndex];
					argumentIndex ++;
					spriteHandler.ChangeSprite(argument, false);
				}
				else if (spriteOperation == SpriteOperation.AnimateOnce)
				{
					var argument = spriteUpdateEntry.arg[argumentIndex];
					argumentIndex ++;
					spriteHandler.AnimateOnce(argument, false);
				}
				else if (spriteOperation == SpriteOperation.PushTexture)
				{
					spriteHandler.PushTexture(false);
				}
				else if (spriteOperation == SpriteOperation.Empty)
				{
					spriteHandler.Empty(false);
				}
				else if (spriteOperation == SpriteOperation.PushClear)
				{
					spriteHandler.PushClear(false);
				}
				else if (spriteOperation == SpriteOperation.ClearPallet)
				{
					spriteHandler.ClearPallet(false);
				}
				else if (spriteOperation == SpriteOperation.SetColour)
				{
					var color = ColorFromArgumentList(spriteUpdateEntry.arg, argumentIndex);
					argumentIndex += 4;
					spriteHandler.SetColor(color, false);
				}
				else if (spriteOperation == SpriteOperation.Pallet)
				{
					var paletteCount = spriteUpdateEntry.arg[argumentIndex];
					var colorList = new List<Color>();
					for (var i = 0; i < paletteCount; i++)
					{
						var color = ColorFromArgumentList(spriteUpdateEntry.arg, argumentIndex);
						argumentIndex += 5;
						colorList.Add(color);
					}
					spriteHandler.SetPaletteOfCurrentSprite(colorList, false);
				}
			}

			return true;
		}

		private static Color ColorFromArgumentList(List<int> argumentList, int argumentIndex)
		{
			var color = Color.white;
			color.r = argumentList[argumentIndex] / 255f;
			color.g = argumentList[argumentIndex + 1] / 255f;
			color.b = argumentList[argumentIndex + 2] / 255f;
			color.a = argumentList[argumentIndex + 3] / 255f;
			return color;
		}

		public static void SendToSpecified(NetworkConnection recipient,
			Dictionary<SpriteHandler, SpriteHandlerManager.SpriteChange> toSend)
		{
			foreach (var changeChunk in toSend.Chunk(500))
			{
				var msg = GenerateMessage(changeChunk);
				SendTo(recipient, msg);
			}
		}

		public static void SendToAll(Dictionary<SpriteHandler, SpriteHandlerManager.SpriteChange> toSend)
		{
			foreach (var changeChunk in toSend.Chunk(500))
			{
				var msg = GenerateMessage(changeChunk);
				SendToAll(msg);
			}
		}


		private static NetMessage GenerateMessage(IEnumerable<KeyValuePair<SpriteHandler, SpriteHandlerManager.SpriteChange>> toSend)
		{
			var msg = new NetMessage();
			GenerateStates(ref msg, toSend);
			return msg;
		}

		private static void GenerateStates(ref NetMessage netMessage,
			IEnumerable<KeyValuePair<SpriteHandler, SpriteHandlerManager.SpriteChange>> toSend)
		{
			var spriteUpdateList = new SpriteUpdateList();
			foreach (var spriteUpdate in toSend)
			{
				var entry = new SpriteUpdateEntry
				{
					id = spriteUpdate.Key.GetMasterNetID().netId,
					name = spriteUpdate.Key.name
				};
				GenerateSerialisation(spriteUpdate.Value, entry);
				spriteUpdateList.list.Add(entry);
			}
			netMessage.SerialiseData = JsonUtility.ToJson(spriteUpdateList);
		}


		private static void GenerateSerialisation(SpriteHandlerManager.SpriteChange spriteChange, SpriteUpdateEntry entry)
		{
			if (spriteChange.PresentSpriteSet != -1)
			{
				entry.call.Add(SpriteOperation.PresentSpriteSet);
				entry.arg.Add(spriteChange.PresentSpriteSet);
			}
			if (spriteChange.VariantIndex != -1)
			{
				entry.call.Add(SpriteOperation.VariantIndex);
				entry.arg.Add(spriteChange.VariantIndex);
			}
			if (spriteChange.CataloguePage != -1)
			{
				entry.call.Add(SpriteOperation.CataloguePage);
				entry.arg.Add(spriteChange.CataloguePage);
			}
			if (spriteChange.AnimateOnce)
			{
				entry.call.Add(SpriteOperation.AnimateOnce);
				entry.arg.Add(spriteChange.CataloguePage);
			}
			if (spriteChange.PushTexture)
			{
				entry.call.Add(SpriteOperation.PushTexture);
			}
			if (spriteChange.Empty)
			{
				entry.call.Add(SpriteOperation.Empty);
			}
			if (spriteChange.PushClear)
			{
				entry.call.Add(SpriteOperation.PushClear);
			}
			if (spriteChange.ClearPallet)
			{
				entry.call.Add(SpriteOperation.ClearPallet);
			}
			if (spriteChange.SetColour != null)
			{
				entry.call.Add(SpriteOperation.SetColour);
				entry.arg.Add(Convert.ToChar(Mathf.RoundToInt(spriteChange.SetColour.Value.r * 255)));
				entry.arg.Add(Convert.ToChar(Mathf.RoundToInt(spriteChange.SetColour.Value.g * 255)));
				entry.arg.Add(Convert.ToChar(Mathf.RoundToInt(spriteChange.SetColour.Value.b * 255)));
				entry.arg.Add(Convert.ToChar(Mathf.RoundToInt(spriteChange.SetColour.Value.a * 255)));
			}
			if (spriteChange.Pallet != null)
			{
				if (spriteChange.Pallet.Count < 1 || spriteChange.Pallet.Count > 255)
				{
					Logger.Log($"Pallet size must be between 1 and 255. It is currently {spriteChange.Pallet.Count}.", Category.Sprites);
					return;
				}
				entry.call.Add(SpriteOperation.Pallet);
				entry.arg.Add(Convert.ToChar(spriteChange.Pallet.Count));
				foreach (var color in spriteChange.Pallet)
				{
					entry.arg.Add(Convert.ToChar(Mathf.RoundToInt(color.r * 255)));
					entry.arg.Add(Convert.ToChar(Mathf.RoundToInt(color.g * 255)));
					entry.arg.Add(Convert.ToChar(Mathf.RoundToInt(color.b * 255)));
					entry.arg.Add(Convert.ToChar(Mathf.RoundToInt(color.a * 255)));
				}
			}
		}

		[Serializable]
		public class SpriteUpdateList
		{
			public List<SpriteUpdateEntry> list = new List<SpriteUpdateEntry>();
		}

		[Serializable]
		public class SpriteUpdateEntry
		{
			public uint id;
			public string name;
			public List<SpriteOperation> call = new List<SpriteOperation>();
			public List<int> arg = new List<int>();
		}

		public enum SpriteOperation
		{
			PresentSpriteSet,
			VariantIndex,
			CataloguePage,
			AnimateOnce,
			PushTexture,
			Empty,
			PushClear,
			ClearPallet,
			SetColour,
			Pallet
		}
	}
}