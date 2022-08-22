using System.Collections.Generic;
using UnityEngine;
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

		public override bool CastSpellServer(PlayerInfo caster, Vector3 destination)
		{
			// Do the actual teleportation here.
			if ((caster.Script.WorldPos - destination).magnitude > MAX_TELEPORT_DISTANCE)
			{
				Chat.AddExamineMsgFromServer(caster.GameObject,
						"Teleporting that distance is too powerful for your skill! Try a smaller distance.");

				return false;
			}

			teleport.ServerTeleportWizard(caster.GameObject, destination.CutToInt());

			return true;
		}

		private void ClientTeleportDestinationSelected(Vector3 position)
		{
			TeleportWindow.gameObject.SetActive(false);

			// We piggyback off aim click instead of using base.CallActionClient();
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestSpell(SpellData.Index, position);
		}

		private void ClientTeleportDestinationSelected(TeleportInfo info)
		{
			ClientTeleportDestinationSelected(info.position);
		}
	}
}
