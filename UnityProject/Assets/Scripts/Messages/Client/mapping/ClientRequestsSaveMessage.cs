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
		public int MatrixID;
		public bool Compact;
		public bool NonmappedItems;
		public LayerType[] Layers;
		public bool SaveObjects;
		public bool CutSection;
	}

	public override void Process(NetMessage msg)
	{
		if (IsFromAdmin() == false) return;
		var Matrix = MatrixManager.Get(msg.MatrixID);

		HashSet<LayerType> Layers = null;
		if (msg.Layers != null)
		{
			Layers = msg.Layers.ToHashSet();
		}

		var Data = MapSaver.MapSaver.SaveMatrix(msg.Compact, Matrix.MetaTileMap, true, msg.Bounds.ToList(),
			msg.NonmappedItems, Layers, msg.SaveObjects, msg.CutSection, msg.PreviewGizmos.ToList());

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

		var StringData = JsonConvert.SerializeObject(Data, settings);

		ServerReturnMapData.Send(SentByPlayer.GameObject, StringData, ServerReturnMapData.MessageType.MapDataFromSave, -1);
	}

	public static NetMessage Send(List<GameGizmoModel> PreviewGizmos, List<BetterBounds> Bounds, MatrixInfo Matrix,
		bool Compact, bool NonmappedItems, HashSet<LayerType> Layers = null, bool SaveObjects = true, bool CutSection = false)
	{
		NetMessage msg = new NetMessage
		{
			PreviewGizmos = PreviewGizmos.ToArray(),
			Bounds = Bounds.ToArray(),
			MatrixID = Matrix.Id,
			Compact = Compact,
			NonmappedItems = NonmappedItems,
			Layers = Layers?.ToArray(),
			SaveObjects = SaveObjects,
			CutSection = CutSection

		};

		Send(msg);
		return msg;
	}
}