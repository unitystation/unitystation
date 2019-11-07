using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;


/// <summary>
/// Singleton. Provides a list of currently enabled occupations and definitions of special occupations
/// </summary>
[CreateAssetMenu(fileName = "OccupationList", menuName = "Singleton/OccupationList")]
public class OccupationList : SingletonScriptableObject<OccupationList>
{
	[FormerlySerializedAs("Occupations")]
	[SerializeField]
	[Tooltip("Allowed occupations")]
	private Occupation[] occcupations;
	public Occupation[] Occupations => occcupations;

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
