using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using US13.Core.SecureStuff.CustomAudio;
using US13.Managers.AddressableManager;
using US13.Messages.Client.Addressable;

namespace US13.Messages.Server.Addressable
{
	public class SendCataloguesToClient : ServerMessage<SendCataloguesToClient.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string serialiseCatalogues;
			public string audioManifest;
		}

		public static ServerAudioManifest ReadManifest(NetMessage message) =>
			ServerAudioManifest.ReadMessage(message.audioManifest, message.serialiseCatalogues);

		public override async void Process(NetMessage message)
		{
			var connection = NetworkClient.connection;
			AddressableCatalogueManager manager = null;
			ServerAudioManifest manifest = null;
			try
			{
				manifest = ReadManifest(message);
				Logs.Loggy.Info($"Received server audio manifest {manifest.Revision}: {manifest.CustomAudio.Count} bundles, {manifest.LobbyTracks.Count} lobby tracks.", Logs.Category.Addressables);
				float deadline = Time.realtimeSinceStartup + 10f;
				while ((manager = AddressableCatalogueManager.FindInstance()) == null)
				{
					if (!NetworkClient.isConnected || !ReferenceEquals(connection, NetworkClient.connection)) return;
					if (Time.realtimeSinceStartup >= deadline)
						throw new InvalidOperationException("No AddressableCatalogueManager exists in the client scene. Check the Managers prefab.");
					await UniTask.NextFrame();
				}
				if (!NetworkClient.isConnected || !ReferenceEquals(connection, NetworkClient.connection)) return;
				bool succeeded = await manager.ApplyServerManifestAsync(manifest);
				if (NetworkClient.isConnected && ReferenceEquals(connection, NetworkClient.connection))
					ClientAudioContentStatus.Send(new ClientAudioContentStatus.NetMessage { Revision = manifest.Revision, Succeeded = succeeded });
			}
			catch (Exception exception)
			{
				// Preserve the stack: an initialization/lifetime failure is not malformed JSON.
				Logs.Loggy.Error($"Failed to apply server audio manifest: {exception}");
				if (!NetworkClient.isConnected || !ReferenceEquals(connection, NetworkClient.connection)) return;
				if (manager == null) manager = AddressableCatalogueManager.FindInstance();
				if (manager != null) manager.ReportServerManifestError(exception.Message);
				else AddressableCatalogueManager.FinishLoaded = true;
				ClientAudioContentStatus.Send(new ClientAudioContentStatus.NetMessage { Revision = manifest?.Revision ?? 0, Succeeded = false });
			}
		}

		public static void BroadcastManifest(ServerAudioManifest manifest)
		{
			AddressableCatalogueManager.ValidateManifest(manifest);
			SendToAll(new NetMessage
			{
				audioManifest = JsonConvert.SerializeObject(manifest),
				serialiseCatalogues = JsonConvert.SerializeObject(manifest.Catalogues)
			});
		}

		public static void SendManifest(ServerAudioManifest manifest, GameObject recipient)
		{
			AddressableCatalogueManager.ValidateManifest(manifest);
			if (recipient == null || !recipient.TryGetComponent<NetworkIdentity>(out var identity) || identity.connectionToClient == null)
			{
				Logs.Loggy.Error("Cannot send server audio manifest: recipient has no client connection.");
				return;
			}
			Logs.Loggy.Info($"Sending server audio manifest {manifest.Revision} to connection {identity.connectionToClient.connectionId}: {manifest.CustomAudio.Count} bundles, {manifest.LobbyTracks.Count} lobby tracks.", Logs.Category.Addressables);
			SendTo(identity.connectionToClient, new NetMessage
			{
				audioManifest = JsonConvert.SerializeObject(manifest),
				serialiseCatalogues = JsonConvert.SerializeObject(manifest.Catalogues)
			});
		}

		public static NetMessage Send(List<string> catalogues, GameObject recipient)
		{
			var message = new NetMessage { serialiseCatalogues = JsonConvert.SerializeObject(catalogues) };
			SendTo(recipient, message);
			return message;
		}
	}
}
