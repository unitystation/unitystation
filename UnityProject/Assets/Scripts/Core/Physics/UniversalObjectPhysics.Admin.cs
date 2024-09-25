﻿using AdminCommands;

namespace Core.Physics
{
	public partial class UniversalObjectPhysics
	{
		private void AdminTeleport()
		{
			AdminCommandsManager.Instance.CmdTeleportToObject(gameObject);
		}

		private void AdminTogglePushable()
		{
			AdminCommandsManager.Instance.CmdTogglePushable(gameObject);
		}
	}
}