using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISetMultitoolBase
{
	MultitoolConnectionType ConType { get; }
}


public interface ISetMultitoolSlave : ISetMultitoolBase
{
	void SetMaster(ISetMultitoolMaster Imaster);
}

public interface ISetMultitoolSlaveMultiMaster : ISetMultitoolBase
{
	void SetMasters(List<ISetMultitoolMaster> Imasters);
}

public interface ISetMultitoolMaster : ISetMultitoolBase
{
	bool MultiMaster { get; }
	void AddSlave(object SlaveObject);
}
