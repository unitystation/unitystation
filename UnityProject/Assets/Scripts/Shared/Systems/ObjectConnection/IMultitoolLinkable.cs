using System.Collections.Generic;
using UnityEngine;

namespace Shared.Systems.ObjectConnection
{
	public enum MultitoolConnectionType
	{
		Empty,
		APC,
		Conveyor,
		BoilerTurbine,
		ReactorChamber,
		FireAlarm,
		LightSwitch,
		DoorButton,
		GeneralSwitch,
		Turret,
		Acu,
		ResearchServer,
		Artifact,
		ResearchLaser
	}

	public interface IMultitoolLinkable
	{
		MultitoolConnectionType ConType { get; }

		GameObject gameObject { get; }
	}

	/// <summary>
	/// Allows a master device to connect slave devices.
	/// </summary>
	public interface IMultitoolMasterable : IMultitoolLinkable
	{
		/// <summary>Whether this connection type supports multiple masters (e.g. two light switches, one light).</summary>
		bool MultiMaster { get; }

		/// <summary>
		/// <para>The maximum distance between a slave and its master allowed for a connection.</para>
		/// <remarks>We limit the distance for gameplay reasons and to ensure reasonable distribution of master controllers.</remarks>
		/// </summary>
		int MaxDistance { get; }
	}

	/// <summary>
	/// Allows a slave device to connect to a master device.
	/// </summary>
	public interface IMultitoolSlaveable : IMultitoolLinkable
	{
		IMultitoolMasterable Master { get; }

		/// <summary>
		/// Try to set the master of the device in-game, via e.g. a multitool. Provides the performer
		/// responsible for the link request.
		/// </summary>
		/// <remarks>Master should never be null and it will always be of the relevant connection type.</remarks>
		/// <param name="performer">The performer of the interaction</param>
		/// <param name="master">Requested master to link with</param>
		/// <returns></returns>
		bool TrySetMaster(GameObject performer, IMultitoolMasterable master);

		/// <summary>Set the master of the device from an editor environment.</summary>
		/// <remarks>The master can be null to indicate an unlinked state.</remarks>
		/// <param name="master">Null for unlinked state</param>
		void SetMasterEditor(IMultitoolMasterable master);

		bool RequireLink { get; }
	}

	/// <summary>
	/// Allows a slave device to connect to multiple master devices.
	/// </summary>
	public interface IMultitoolMultiMasterSlaveable : IMultitoolLinkable
	{
		void SetMasters(List<IMultitoolMasterable> iMasters);
	}
}
