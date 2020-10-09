using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Messages.Client;
using Systems.Teleport;
using UI.Core.Windows;

namespace Systems.Spells.Wizard
{
	public class TeleportAction : Spell
	{
		// No stations should be larger than 1000.
		// TODO: Consider increasing this distance when the spell is upgraded.
		private const int MAX_TELEPORT_DISTANCE = 1000;

		private TeleportSpell teleport;

		[Tooltip("Assign the teleport window prefab here")]
		[SerializeField]
		private GameObject teleportWindowPrefab = default;

		private static Dictionary<ConnectedPlayer, Vector3Int> requestedTeleports = new Dictionary<ConnectedPlayer, Vector3Int>();

		private TeleportWindow TeleportWindow => UIManager.TeleportWindow;

		private void Awake()
		{
			teleport = GetComponent<TeleportSpell>();
		}

		public override void CallActionClient()
		{
			// TODO: Have action button sprite static until clicked.
			// Animation then begins until destination selected. Cooldown begins.
			TeleportWindow.SetWindowTitle("Teleport to Place");
			TeleportWindow.gameObject.SetActive(true);
			TeleportWindow.GenerateButtons(TeleportUtils.GetSpawnDestinations());

			TeleportWindow.onTeleportRequested += ClientTeleportDestinationSelected;
			TeleportWindow.onTeleportToVector += ClientTeleportDestinationSelected;
		}

		public static void ServerProcessTeleportRequest(ConnectedPlayer caster, Vector3Int destination)
		{
			if (requestedTeleports.ContainsKey(caster))
			{
				requestedTeleports[caster] = destination;
			}
			else
			{
				requestedTeleports.Add(caster, destination);
			}
		}

		public override bool CastSpellServer(ConnectedPlayer caster)
		{
			// Do the actual teleportation here.
			if (!requestedTeleports.ContainsKey(caster)) return false;
			
			Vector3Int destination = requestedTeleports[caster];
			if ((caster.Script.WorldPos - destination).magnitude > MAX_TELEPORT_DISTANCE)
			{
				Chat.AddExamineMsgFromServer(caster.GameObject,
						"Teleporting that distance is too powerful for your skill! Try a smaller distance.");

				return false;
			}

			teleport.ServerTeleportWizard(caster.GameObject, destination);

			return true;
		}

		private void ClientTeleportDestinationSelected(Vector3 position)
		{
			TeleportWindow.gameObject.SetActive(false);

			// We run our own instead of base.CallActionClient();
			ClientRequestTeleportMessage.Send(SpellData.Index, position.CutToInt());
		}

		private void ClientTeleportDestinationSelected(TeleportInfo info)
		{
			ClientTeleportDestinationSelected(info.position);
		}
	}

	// We use NetMessage to request the teleport. That way we can run the spell with data.
	// This isn't a good solution as we want to be able to have spells that send other information,
	// like click position.
	public class ClientRequestTeleportMessage : ClientMessage
	{
		public int SpellIndex;
		public Vector3Int Destination;

		public override void Process()
		{
			TeleportAction.ServerProcessTeleportRequest(SentByPlayer, Destination);

			Spell teleport = SentByPlayer.Script.mind.Spells.First(spell => spell.SpellData.Index == SpellIndex);
			teleport.CallActionServer(SentByPlayer);
		}

		public static ClientRequestTeleportMessage Send(int spellIndex, Vector3Int destination)
		{
			ClientRequestTeleportMessage msg = new ClientRequestTeleportMessage
			{
				SpellIndex = spellIndex,
				Destination = destination,
			};

			msg.Send();
			return msg;
		}
	}
}
