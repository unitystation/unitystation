using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Systems.Electricity;
using Systems.ObjectConnection;


namespace Items.Engineering
{
	public class Multitool : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, IInteractable<HandActivate>
	{
		private bool isMultipleMaster = false;
		private MultitoolConnectionType configurationBuffer = MultitoolConnectionType.Empty;

		private readonly List<IMultitoolMasterable> buffers = new List<IMultitoolMasterable>();
		private IMultitoolMasterable Buffer => buffers.Count > 0 ? buffers[0] : null;

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			// Use default interaction checks
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (Validations.IsTarget(gameObject, interaction)) return false;

			return true;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			if (interaction.TargetObject != null)
			{
				var multitoolBases = interaction.TargetObject.GetComponents<IMultitoolLinkable>();
				foreach (var multitoolBase in multitoolBases)
				{
					if (Buffer == null || isMultipleMaster)
					{
						if (multitoolBase is IMultitoolMasterable master)
						{
							configurationBuffer = master.ConType;
							buffers.Add(master);
							isMultipleMaster = master.MultiMaster;
							Chat.AddExamineMsgFromServer(
								interaction.Performer,
								$"You add the <b>{interaction.TargetObject.ExpensiveName()}</b> to the multitool's master buffer.");
							return;
						}
					}

					if (Buffer == null) continue;
					if (configurationBuffer != multitoolBase.ConType) continue;

					var slaveComponent = Buffer as Component;
					if (Vector3.Distance(slaveComponent.transform.position, interaction.TargetObject.transform.position) > Buffer.MaxDistance)
					{
						Chat.AddExamineMsgFromServer(
							interaction.Performer,
							$"This device is too far away from the master device <b>{slaveComponent.gameObject.ExpensiveName()}!");
						return;
					}

					switch (multitoolBase)
					{
						case IMultitoolSlaveable slave:
							if (slave.TrySetMaster(interaction, Buffer))
							{
								Chat.AddExamineMsgFromServer(
								interaction.Performer,
								$"You connect the <b>{interaction.TargetObject.ExpensiveName()}</b> " +
								$"to the master device <b>{slaveComponent.gameObject.ExpensiveName()}</b>.");
							}							
							return;
						case IMultitoolMultiMasterSlaveable slaveMultiMaster:
							slaveMultiMaster.SetMasters(buffers);
							Chat.AddExamineMsgFromServer(
								interaction.Performer,
								$"You connect the <b>{interaction.TargetObject.ExpensiveName()}</b> to the master devices in the buffer.");
							return;
						default:
							Chat.AddExamineMsgFromServer(
								interaction.Performer,
								"This only seems to have the capability of <b>writing</b> to the buffer.");
							return;
					}
				}
			}

			PrintElectricalThings(interaction);
		}

		public void PrintElectricalThings(PositionalHandApply interaction)
		{
			Vector3Int worldPosInt = interaction.WorldPositionTarget.To2Int().To3Int();
			MatrixInfo matrixinfo = MatrixManager.AtPoint(worldPosInt, true);
			var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrixinfo);
			var matrix = interaction.Performer.GetComponentInParent<Matrix>();
			var electricalNodes = matrix.GetElectricalConnections(localPosInt);

			APCPoweredDevice device = default;
			bool deviceFound = interaction.TargetObject != null && interaction.TargetObject.TryGetComponent(out device);

			StringBuilder sb = new StringBuilder("The multitool couldn't find anything electrical here.");
			if (deviceFound || electricalNodes.Count > 0)
			{
				sb.Clear();
				sb.AppendLine("The multitool's display lights up.</i>");
				
				if (deviceFound)
				{
					sb.AppendLine(device.RelatedAPC == null
							? $"<b>{device.gameObject.ExpensiveName()}</b> is not connected to an APC!"
							: $"<b>{device.gameObject.ExpensiveName()}</b>: {device.Wattusage.ToEngineering("W")} " +
									$"({device.RelatedAPC.Voltage.ToEngineering("V")})");
				}
				foreach (var node in electricalNodes)
				{
					sb.AppendLine(node.ShowInGameDetails());
				}

				sb.Append("<i>");
			}

			electricalNodes.Clear();
			ElectricalPool.PooledFPCList.Add(electricalNodes);
			Chat.AddExamineMsgFromServer(interaction.Performer, sb.ToString());
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "You clear the multitool's internal buffer.");
			buffers.Clear();
			isMultipleMaster = false;
			configurationBuffer = MultitoolConnectionType.Empty;
		}
	}
}
