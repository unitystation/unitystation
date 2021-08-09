using System.Collections.Generic;


namespace Systems.ObjectConnection
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
	}

	public interface ISetMultitoolBase
	{
		MultitoolConnectionType ConType { get; }
	}

	public interface ISetMultitoolSlave : ISetMultitoolBase
	{
		void SetMaster(ISetMultitoolMaster iMaster);
	}

	public interface ISetMultitoolSlaveMultiMaster : ISetMultitoolBase
	{
		void SetMasters(List<ISetMultitoolMaster> iMasters);
	}

	public interface ISetMultitoolMaster : ISetMultitoolBase
	{
		/// <summary>Whether this connection type supports multiple masters (e.g. two light switches, one light).</summary>
		bool MultiMaster { get; }
		/// <summary>The maximum distance a slave can be from the master for a connection.</summary>
		int MaxDistance { get; }

		void AddSlave(object slaveObject);
	}
}
