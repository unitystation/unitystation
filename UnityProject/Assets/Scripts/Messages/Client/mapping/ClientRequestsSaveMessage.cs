using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using Messages.Client;
using Mirror;
using Newtonsoft.Json;
using TileManagement;
using UnityEngine;

public class ClientRequestsSaveMessage : ClientMessage<ClientRequestsSaveMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public GameGizmoModel[] PreviewGizmos;
		public BetterBounds[] Bounds;
		public int[] MatrixIDs;
		public bool Compact;
		public bool NonmappedItems;
		public LayerType[] Layers;
		public bool SaveObjects;
		public bool CutSection;
		public string MapName;
	}

	public override void Process(NetMessage msg)
	{
		if (HasPermission(TAG.MAP_SAVE) == false) return;

		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore, // Ignore null values
			DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, // Ignore default values
			Formatting = Formatting.Indented
		};

		if (msg.Compact)
		{
			settings.Formatting = Formatting.None;
		}

		if (msg.MatrixIDs.Length > 1)
		{
			var Matrix = msg.MatrixIDs.Select(x => MatrixManager.Get(x).MetaTileMap).ToList();
			var Data = MapSaver.MapSaver.SaveMap(Matrix, msg.Compact, msg.MapName);

			var StringData = JsonConvert.SerializeObject(Data, settings);

			ServerReturnMapData.Send(SentByPlayer.GameObject, StringData, ServerReturnMapData.MessageType.MapDataFromSave, -1);

		}
		else
		{
			var Matrix = MatrixManager.Get(msg.MatrixIDs.First());

			HashSet<LayerType> Layers = null;
			if (msg.Layers != null)
			{
				Layers = msg.Layers.ToHashSet();
			}

			var Data = MapSaver.MapSaver.SaveMatrix(msg.Compact, Matrix.MetaTileMap, true, msg.Bounds.ToList(),
				msg.NonmappedItems, Layers, msg.SaveObjects, msg.CutSection, msg.PreviewGizmos.ToList());



			var StringData = JsonConvert.SerializeObject(Data, settings);

			ServerReturnMapData.Send(SentByPlayer.GameObject, StringData, ServerReturnMapData.MessageType.MapDataFromSave, -1);
		}
	}

	public static NetMessage Send(List<GameGizmoModel> PreviewGizmos, List<BetterBounds> Bounds,  List<MatrixInfo> Matrixs,
		bool Compact, bool NonmappedItems, HashSet<LayerType> Layers = null, bool SaveObjects = true, bool CutSection = false, string MapName = "")
	{
		NetMessage msg = new NetMessage
		{
			PreviewGizmos = PreviewGizmos.ToArray(),
			Bounds = Bounds.ToArray(),
			MatrixIDs = Matrixs.Select(x => x.Id).ToArray(),
			Compact = Compact,
			NonmappedItems = NonmappedItems,
			Layers = Layers?.ToArray(),
			SaveObjects = SaveObjects,
			CutSection = CutSection,
			MapName = MapName
		};

		Send(msg);
		return msg;
	}
}