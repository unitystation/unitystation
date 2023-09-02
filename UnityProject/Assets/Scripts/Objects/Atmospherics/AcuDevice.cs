using Logs;
using UnityEngine;
using Systems.Atmospherics;
using Shared.Systems.ObjectConnection;

namespace Objects.Atmospherics
{
	/// <summary>Allows an <seealso cref="AcuDevice"/> to be used for air quality sampling and control.</summary>
	public interface IAcuControllable
	{
		/// <summary>The atmospheric sample the device reports. Typically is for the device's tile.</summary>
		AcuSample AtmosphericSample { get; }

		/// <summary>
		/// The operating mode the controlling <see cref="AirController"/> has indicated the device should operate with.
		/// <para>Each device type is responsible for interpreting their own behaviour from the <c>ACU</c>'s operating mode.</para>
		/// </summary>
		void SetOperatingMode(AcuMode mode);
	}

	/// <summary>
	/// Allows an object with this component to be controlled by an <seealso cref="AirController"/>.
	/// <para>When the object is initialised by the server, the referenced (mapped) <c>ACU</c> connects to the device.</para>
	/// <para>See also <seealso cref="CustomInspectors.SlaveDeviceInspector"/>.</para>
	/// </summary>
	public class AcuDevice : MonoBehaviour, IServerLifecycle, IMultitoolSlaveable
	{
		/// <summary>The controller this device should link with at server initialisation.</summary>
		public AirController Controller;

		private IAcuControllable device;

		private void Awake()
		{
			device = GetComponent<IAcuControllable>();
			if (device == null)
			{
				Loggy.LogError($"{this} has no component that implements {nameof(IAcuControllable)}!");
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (Controller == null) return;
			Controller.AddSlave(device);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			if (Controller == null) return;
			Controller.RemoveSlave(device);
		}

		#region Multitool Interaction

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.Acu;
		bool IMultitoolSlaveable.RequireLink => true;
		IMultitoolMasterable IMultitoolSlaveable.Master => Controller;

		bool IMultitoolSlaveable.TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			SetMaster(master);

			if (Controller != null)
			{
				Controller.AddSlave(device);
			}

			return true;
		}

		void IMultitoolSlaveable.SetMasterEditor(IMultitoolMasterable master)
		{
			SetMaster(master);
		}

		private void SetMaster(IMultitoolMasterable master)
		{
			// Disconnect any prior controller connection.
			if (Controller != null)
			{
				Controller.RemoveSlave(device);
			}

			Controller = (AirController) master;
		}

		#endregion
	}
}
