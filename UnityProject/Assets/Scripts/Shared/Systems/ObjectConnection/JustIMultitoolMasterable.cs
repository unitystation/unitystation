using System.Collections;
using System.Collections.Generic;
using Shared.Systems.ObjectConnection;
using UnityEngine;

public class JustIMultitoolMasterable : MonoBehaviour, IMultitoolMasterable
{
	public bool IsMultiMaster;

	/// <summary>Whether this connection type supports multiple masters (e.g. two light switches, one light).</summary>
	public bool MultiMaster => IsMultiMaster;

	public int SetMaxDistance;

	/// <summary>
	/// <para>The maximum distance between a slave and its master allowed for a connection.</para>
	/// <remarks>We limit the distance for gameplay reasons and to ensure reasonable distribution of master controllers.</remarks>
	/// </summary>
	public int MaxDistance => SetMaxDistance;

	public MultitoolConnectionType SetConType;

	public MultitoolConnectionType ConType => SetConType;
}
