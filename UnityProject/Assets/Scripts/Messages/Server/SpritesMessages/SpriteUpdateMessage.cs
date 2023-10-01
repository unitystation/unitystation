using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Mirror;
using UnityEngine;

namespace Messages.Server.SpritesMessages
{
	public class SpriteUpdateMessage : ServerMessage<SpriteUpdateMessage.NetMessage>
	{
		public static readonly List<SpriteUpdateEntry> UnprocessedData = new List<SpriteUpdateEntry>();

		public struct NetMessage : NetworkMessage
		{
			public List<KeyValuePair<SpriteHandler, SpriteHandlerManager.SpriteChange>> Data;
			public List<KeyValuePair<string, SpriteHandlerManager.SpriteChange>> DataSpecial;
			//public string SerialiseData;
		}

		public override void Process(NetMessage msg)
		{
			if (CustomNetworkManager.Instance._isServer)
				return;

			List<SpriteUpdateEntry> spriteUpdateList = new List<SpriteUpdateEntry>();

			spriteUpdateList.AddRange(UnprocessedData);
			for (var i = spriteUpdateList.Count - 1; i >= 0; i--)
			{
				var spriteUpdateEntry = spriteUpdateList[i];
				if (ProcessEntry(spriteUpdateEntry))
				{
					spriteUpdateList.Remove(spriteUpdateEntry);
				}
			}

			UnprocessedData.Clear();
			UnprocessedData.AddRange(spriteUpdateList);
		}

		private bool ProcessEntry(SpriteUpdateEntry spriteUpdateEntry)
		{
			var spawned = CustomNetworkManager.Spawned;
			if (spawned.TryGetValue(spriteUpdateEntry.id, out var networkIdentity) == false) return false;
			if (networkIdentity == null) return false;

			if (SpriteHandlerManager.PresentSprites.ContainsKey(networkIdentity) == false ||
			    SpriteHandlerManager.PresentSprites[networkIdentity].ContainsKey(spriteUpdateEntry.name) == false)
			{
				return false;
			}

			var spriteHandler = SpriteHandlerManager.PresentSprites[networkIdentity][spriteUpdateEntry.name];
			var argumentIndex = 0;
			foreach (var spriteOperation in spriteUpdateEntry.call)
			{
				if (spriteOperation == SpriteOperation.PresentSpriteSet)
				{
					var argument = spriteUpdateEntry.arg[argumentIndex];
					argumentIndex++;
					spriteHandler.SetSpriteSO(SpriteCatalogue.ResistantCatalogue[argument], networked: false);
				}
				else if (spriteOperation == SpriteOperation.VariantIndex)
				{
					var argument = spriteUpdateEntry.arg[argumentIndex];
					argumentIndex++;
					spriteHandler.ChangeSpriteVariant(argument, false);
				}
				else if (spriteOperation == SpriteOperation.CataloguePage)
				{
					var argument = spriteUpdateEntry.arg[argumentIndex];
					argumentIndex++;
					spriteHandler.ChangeSprite(argument, false);
				}
				else if (spriteOperation == SpriteOperation.AnimateOnce)
				{
					var argument = spriteUpdateEntry.arg[argumentIndex];
					argumentIndex++;
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
					spriteHandler.ClearPalette(false);
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
			foreach (var changeChunk in toSend.Chunk(2000))
			{
				var msg = GenerateMessage(changeChunk);
				SendTo(recipient, msg);
			}
		}

		public static void SendToSpecified(NetworkConnection recipient,
			Dictionary<string, SpriteHandlerManager.SpriteChange> toSend)
		{
			foreach (var changeChunk in toSend.Chunk(2000))
			{
				var msg = GenerateMessage(changeChunk);
				SendTo(recipient, msg);
			}
		}

		public static void SendToAll(Dictionary<SpriteHandler, SpriteHandlerManager.SpriteChange> toSend)
		{
			foreach (var changeChunk in toSend.Chunk(2000))
			{
				var msg = GenerateMessage(changeChunk);
				SendToAll(msg);
			}
		}

		public static void SendToAll(Dictionary<string, SpriteHandlerManager.SpriteChange> toSend)
		{
			foreach (var changeChunk in toSend.Chunk(2000))
			{
				var msg = GenerateMessage(changeChunk);
				SendToAll(msg);
			}
		}


		private static NetMessage GenerateMessage(
			IEnumerable<KeyValuePair<string, SpriteHandlerManager.SpriteChange>> toSend)
		{
			var msg = new NetMessage();
			msg.DataSpecial = toSend.ToList();
			return msg;
		}

		private static NetMessage GenerateMessage(
			IEnumerable<KeyValuePair<SpriteHandler, SpriteHandlerManager.SpriteChange>> toSend)
		{
			var msg = new NetMessage();
			msg.Data = toSend.ToList();
			return msg;
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
			PresentSpriteSet = 1,
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

	public static class SpriteUpdateMessageReaderWriters
	{
		public enum SpriteOperation
		{
			PresentSpriteSet = 1,
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

		public static SpriteUpdateMessage.NetMessage Deserialize(this NetworkReader reader)
		{
			var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;
			var message = new SpriteUpdateMessage.NetMessage();
			SpriteUpdateMessage.SpriteUpdateEntry UnprocessedData = null;
			while (true)
			{
				UnprocessedData = null;
				bool ProcessSection = true;
				bool SkipSection = false;

				uint NetID = reader.ReadUInt();
				if (NetID == 0)
				{
					break;
				}

				if ((spawned.ContainsKey(NetID) == false || spawned[NetID] == null) && NetID != NetId.Invalid)
				{
					ProcessSection = false;
				}

				string Name = reader.ReadString();
				if (NetID != NetId.Invalid)
				{
					if (ProcessSection == false ||
					    spawned.ContainsKey(NetID) == false ||
					    SpriteHandlerManager.PresentSprites.ContainsKey(spawned[NetID]) == false ||
					    SpriteHandlerManager.PresentSprites[spawned[NetID]].ContainsKey(Name) == false)
					{
						ProcessSection = false;
					}
				}
				else
				{
					if (SpriteHandlerManager.SpecialPresentSprites.ContainsKey(Name) == false || SpriteHandlerManager.SpecialPresentSprites[Name] == null)
					{
						ProcessSection = false;
						SkipSection = true;
					}
				}



				if (ProcessSection == false)
				{
					UnprocessedData = new SpriteUpdateMessage.SpriteUpdateEntry();
					UnprocessedData.name = Name;
					UnprocessedData.id = NetID;
				}

				SpriteHandler SP = null;
				if (ProcessSection)
				{
					if (NetID != NetId.Invalid)
					{
						SP = SpriteHandlerManager.PresentSprites[spawned[NetID]][Name];
					}
					else
					{
						SP = SpriteHandlerManager.SpecialPresentSprites[Name];
					}
				}


				while (true)
				{
					byte Operation = reader.ReadByte();

					if (Operation == 255)
					{
						if (ProcessSection == false && SkipSection == false)
						{
							SpriteUpdateMessage.UnprocessedData.Add(UnprocessedData);
						}

						break;
					}

					if (Operation == (byte) SpriteOperation.PresentSpriteSet)
					{
						int SpriteID = reader.ReadInt();
						if (ProcessSection)
						{
							try
							{
								SP.SetSpriteSO(SpriteCatalogue.ResistantCatalogue[SpriteID], networked: false);
							}
							catch (Exception e)
							{
								Loggy.Log(e.ToString());
							}
						}
						else
						{
							UnprocessedData.call.Add(SpriteUpdateMessage.SpriteOperation.PresentSpriteSet);
							UnprocessedData.arg.Add(SpriteID);
						}
					}


					if (Operation == (byte) SpriteOperation.VariantIndex)
					{
						int Variant = reader.ReadInt();
						if (ProcessSection)
						{
							try
							{
								SP.ChangeSpriteVariant(Variant, networked: false);
							}
							catch (Exception e)
							{
								Loggy.Log(e.ToString());
							}

						}
						else
						{
							UnprocessedData.call.Add(SpriteUpdateMessage.SpriteOperation.VariantIndex);
							UnprocessedData.arg.Add(Variant);
						}
					}

					if (Operation == (byte) SpriteOperation.CataloguePage)
					{
						int Sprite = reader.ReadInt();
						if (ProcessSection)
						{
							try
							{
								SP.ChangeSprite(Sprite, false);
							}
							catch (Exception e)
							{
								Loggy.Log(e.ToString());
							}

						}
						else
						{
							UnprocessedData.call.Add(SpriteUpdateMessage.SpriteOperation.CataloguePage);
							UnprocessedData.arg.Add(Sprite);
						}
					}

					if (Operation == (byte) SpriteOperation.AnimateOnce)
					{
						int SpriteAnimate = reader.ReadInt();
						if (ProcessSection)
						{
							try
							{
								SP.AnimateOnce(SpriteAnimate, false);
							}
							catch (Exception e)
							{
								Loggy.Log(e.ToString());
							}
						}
						else
						{
							UnprocessedData.call.Add(SpriteUpdateMessage.SpriteOperation.AnimateOnce);
							UnprocessedData.arg.Add(SpriteAnimate);
						}
					}

					if (Operation ==  (byte) SpriteOperation.PushTexture)
					{
						if (ProcessSection)
						{
							try
							{
								SP.PushTexture(false);
							}
							catch (Exception e)
							{
								Loggy.Log(e.ToString());
							}
						}
						else
						{
							UnprocessedData.call.Add(SpriteUpdateMessage.SpriteOperation.PushTexture);
						}
					}

					if (Operation == (byte) SpriteOperation.Empty)
					{
						if (ProcessSection)
						{
							try
							{
								SP.Empty(false);
							}
							catch (Exception e)
							{
								Loggy.Log(e.ToString());
							}

						}
						else
						{
							UnprocessedData.call.Add(SpriteUpdateMessage.SpriteOperation.Empty);
						}
					}


					if (Operation == (byte) SpriteOperation.PushClear)
					{
						if (ProcessSection)
						{
							try
							{
								SP.PushClear(false);
							}
							catch (Exception e)
							{
								Loggy.Log(e.ToString());
							}
						}
						else
						{
							UnprocessedData.call.Add(SpriteUpdateMessage.SpriteOperation.PushClear);
						}
					}

					if (Operation == (byte) SpriteOperation.ClearPallet)
					{
						if (ProcessSection)
						{
							try
							{
								SP.ClearPalette(false);
							}
							catch (Exception e)
							{
								Loggy.Log(e.ToString());
							}
						}
						else
						{
							UnprocessedData.call.Add(SpriteUpdateMessage.SpriteOperation.ClearPallet);
						}
					}


					if (Operation == (byte) SpriteOperation.SetColour)
					{
						Color TheColour = reader.ReadColor();
						if (ProcessSection)
						{
							if (SP)
							{
								try
								{
									//TODO: remove this check - registering arrives after the sprite update, all clients will disconnect after a runtime
									//removing and readding a bodypart through surgery would cause it, since the network identity already exists unlike the creation of a new human
									SP.SetColor(TheColour, false);
								}
								catch (Exception e)
								{
									Loggy.Log(e.ToString());
								}

							}
						}
						else
						{
							UnprocessedData.call.Add(SpriteUpdateMessage.SpriteOperation.SetColour);
							UnprocessedData.arg.Add(Convert.ToChar(Mathf.RoundToInt(TheColour.r * 255)));
							UnprocessedData.arg.Add(Convert.ToChar(Mathf.RoundToInt(TheColour.g * 255)));
							UnprocessedData.arg.Add(Convert.ToChar(Mathf.RoundToInt(TheColour.b * 255)));
							UnprocessedData.arg.Add(Convert.ToChar(Mathf.RoundToInt(TheColour.a * 255)));
						}
					}

					if (Operation == (byte) SpriteOperation.Pallet)
					{
						int paletteCount = reader.ReadByte();
						List<Color> Colours = new List<Color>();
						for (int i = 0; i < paletteCount; i++)
						{
							Colours.Add(reader.ReadColor());
						}

						if (ProcessSection)
						{
							try
							{
								SP.SetPaletteOfCurrentSprite(Colours, false);
							}
							catch (Exception e)
							{
								Loggy.Log(e.ToString());
							}
						}
						else
						{
							UnprocessedData.call.Add(SpriteUpdateMessage.SpriteOperation.Pallet);
							UnprocessedData.arg.Add(Convert.ToChar(paletteCount));
							foreach (var color in Colours)
							{
								UnprocessedData.arg.Add(Convert.ToChar(Mathf.RoundToInt(color.r * 255)));
								UnprocessedData.arg.Add(Convert.ToChar(Mathf.RoundToInt(color.g * 255)));
								UnprocessedData.arg.Add(Convert.ToChar(Mathf.RoundToInt(color.b * 255)));
								UnprocessedData.arg.Add(Convert.ToChar(Mathf.RoundToInt(color.a * 255)));
							}
						}
					}
				}
			}

			return message;
		}

		public static void Serialize(this NetworkWriter writer, SpriteUpdateMessage.NetMessage message)
		{
			if (message.Data != null)
			{
				foreach (var keyValuePair in message.Data)
				{
					var netid = keyValuePair.Key.GetMasterNetID();
					if (netid.netId == 0) continue; //If it is zero it is invalid and will also cause the network message to explode and die
					var spriteChange = keyValuePair.Value;
					writer.WriteUInt(netid.netId);
					writer.WriteString(keyValuePair.Key.name);
					RightChange(writer, spriteChange);
				}
			}


			if (message.DataSpecial != null)
			{
				foreach (var keyValuePair in message.DataSpecial)
				{
					var spriteChange = keyValuePair.Value;
					writer.WriteUInt(NetId.Invalid);
					writer.WriteString(keyValuePair.Key);
					RightChange(writer, spriteChange);
				}
			}


			writer.WriteUInt(0);
		}


		private static void RightChange(NetworkWriter writer, SpriteHandlerManager.SpriteChange spriteChange)
		{
			if (spriteChange.PresentSpriteSet != -1)
			{
				writer.WriteByte((byte) SpriteOperation.PresentSpriteSet);
				writer.WriteInt(spriteChange.PresentSpriteSet);
			}

			if (spriteChange.VariantIndex != -1)
			{
				writer.WriteByte((byte) SpriteOperation.VariantIndex);
				writer.WriteInt(spriteChange.VariantIndex);
			}

			if (spriteChange.CataloguePage != -1)
			{
				writer.WriteByte((byte) SpriteOperation.CataloguePage);
				writer.WriteInt(spriteChange.CataloguePage);
			}

			if (spriteChange.AnimateOnce)
			{
				writer.WriteByte((byte) SpriteOperation.AnimateOnce);
				writer.WriteInt(spriteChange.CataloguePage);
			}

			if (spriteChange.PushTexture)
			{
				writer.WriteByte((byte) SpriteOperation.PushTexture);
			}

			if (spriteChange.Empty)
			{
				writer.WriteByte((byte) SpriteOperation.Empty);
			}

			if (spriteChange.PushClear)
			{
				writer.WriteByte((byte) SpriteOperation.PushClear);
			}

			if (spriteChange.ClearPalette)
			{
				writer.WriteByte((byte) SpriteOperation.ClearPallet);
			}

			if (spriteChange.SetColour != null)
			{
				writer.WriteByte((byte) SpriteOperation.SetColour);
				writer.WriteColor(spriteChange.SetColour.Value);
			}

			if (spriteChange.Palette != null)
			{
				writer.WriteByte((byte) SpriteOperation.Pallet);
				writer.WriteByte((byte) spriteChange.Palette.Count);
				foreach (Color Colour in spriteChange.Palette)
				{
					writer.WriteColor(Colour);
				}
			}
			writer.WriteByte((byte) 255);
		}
	}
}
