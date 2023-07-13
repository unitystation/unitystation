using System.Collections.Generic;
using System.Linq;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Serialization;


/// <summary>
/// Singleton. Provides a list of currently enabled occupations and the order in which they
/// should appear in the chooser.
/// </summary>
[CreateAssetMenu(fileName = "OccupationListSingleton", menuName = "Singleton/OccupationList")]
public class OccupationList : SingletonScriptableObject<OccupationList>
{
	[FormerlySerializedAs("Occupations")]
	[SerializeField]
	[Tooltip("Allowed occupations, and the order in which they should be displayed in" +
	         " occupation chooser.")]
	private Occupation[] occcupations = null;

	[SerializeField] [Tooltip("All of the occupations used for stuff like spectator")]
	public Occupation[] AllOcccupations = new Occupation[0];

	public IEnumerable<Occupation> Occupations => occcupations;

	/// <summary>
	/// Gets the occupation with the specified jobtype. Null if not in this list.
	/// </summary>
	/// <param name="jobType"></param>
	/// <returns></returns>
	public Occupation Get(JobType jobType)
	{
		return occcupations.FirstOrDefault(ocp => ocp.JobType == jobType);
	}
}
