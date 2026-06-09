using System.Collections.Generic;
using System.Linq;
using Mirror;
using Newtonsoft.Json;
using US13.Core.GameGizmos;
using US13.Managers.MatrixManager;
using US13.Messages.Client.Admin;
using US13.Messages.Server.Mapping;
using US13.Shuttles;
using US13.Tilemaps.Utils;

namespace US13.Messages.Client.mapping
{
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
			public string MapSAVEName;
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


				var Matrix = msg.MatrixIDs.Select(x => MatrixManager.Get(x).MetaTileMap).Where(x =>x.matrix != MatrixManager.Instance.spaceMatrix).ToList();

				if (string.IsNullOrEmpty(msg.MapName))
				{
					msg.MapName = Matrix.First().matrix.MatrixInfo.Name;
				}

				var Data = MapSaver.MapSaver.SaveMap(Matrix, msg.Compact, msg.MapName);

				var StringData = JsonConvert.SerializeObject(Data, settings);


				if (string.IsNullOrEmpty(msg.MapSAVEName))
				{
					ServerReturnMapData.Send(SentByPlayer.GameObject, StringData, ServerReturnMapData.MessageType.MapDataFromSave, -1);
				}
				else
				{
					ServerReturnMapData.Send(SentByPlayer.GameObject, StringData, ServerReturnMapData.MessageType.MapSaveName, -1, msg.MapSAVEName);
				}
			}
			else if (msg.MatrixIDs.Length == 1)
			{
				var Matrix = MatrixManager.Get(msg.MatrixIDs.First());
				if (Matrix.Matrix == MatrixManager.Instance.spaceMatrix)
				{
					return;
				}
				HashSet<LayerType> Layers = null;
				if (msg.Layers != null)
				{
					Layers = msg.Layers.ToHashSet();
				}

				if (string.IsNullOrEmpty(msg.MapSAVEName))
				{
					var Data = MapSaver.MapSaver.SaveMatrix(msg.Compact, Matrix.MetaTileMap, true, msg.Bounds.ToList(),
						msg.NonmappedItems, Layers, msg.SaveObjects, msg.CutSection, msg.PreviewGizmos.ToList());

					var StringData = JsonConvert.SerializeObject(Data, settings);
					ServerReturnMapData.Send(SentByPlayer.GameObject, StringData,
						ServerReturnMapData.MessageType.MapDataFromSave, -1);
				}
				else
				{
					var Matrixs = msg.MatrixIDs.Select(x => MatrixManager.Get(x).MetaTileMap).Where(x =>x.matrix != MatrixManager.Instance.spaceMatrix).ToList();
					var Data = MapSaver.MapSaver.SaveMap(Matrixs, msg.Compact, msg.MapName);
					var StringData = JsonConvert.SerializeObject(Data, settings);
					ServerReturnMapData.Send(SentByPlayer.GameObject, StringData,
						ServerReturnMapData.MessageType.MapSaveName, -1, msg.MapSAVEName);
				}
			}
			else
			{

				var Matrix = MatrixManager.Instance.ActiveMatricesList.Where(x => MatrixManager.Instance.spaceMatrix != x.Matrix).Select(x => x.Matrix.MetaTileMap).ToList();

				if (string.IsNullOrEmpty(msg.MapName))
				{
					msg.MapName = Matrix.First().matrix.MatrixInfo.Name;
				}
				var Data = MapSaver.MapSaver.SaveMap(Matrix, msg.Compact, msg.MapName);

				var StringData = JsonConvert.SerializeObject(Data, settings);
				if (string.IsNullOrEmpty(msg.MapSAVEName))
				{
					ServerReturnMapData.Send(SentByPlayer.GameObject, StringData, ServerReturnMapData.MessageType.MapDataFromSave, -1);
				}
				else
				{
					ServerReturnMapData.Send(SentByPlayer.GameObject, StringData, ServerReturnMapData.MessageType.MapSaveName, -1, msg.MapSAVEName);
				}
			}
		}

		public static NetMessage Send(List<GameGizmoModel> PreviewGizmos, List<BetterBounds> Bounds,  List<MatrixInfo> Matrixs,
			bool Compact, bool NonmappedItems, HashSet<LayerType> Layers = null, bool SaveObjects = true, bool CutSection = false, string MapName = "", string MapSAVEName = "")
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
				MapName = MapName,
				MapSAVEName = MapSAVEName
			};

			Send(msg);
			return msg;
		}
	}
}