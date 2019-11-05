using System.Linq;
using UnityEngine;


/// <summary>
/// Singleton. Provides a list of currently enabled occupations and definitions of special occupations
/// </summary>
[CreateAssetMenu(fileName = "OccupationList", menuName = "Singleton/OccupationList")]
public class OccupationList : SingletonScriptableObject<OccupationList>
{
	[Tooltip("Allowed occupations")]
	public Occupation[] Occupations;

	/// <summary>
	/// Gets the occupation with the specified jobtype. Null if not in this list.
	/// </summary>
	/// <param name="jobType"></param>
	/// <returns></returns>
	public Occupation Get(JobType jobType)
	{
		return Occupations.FirstOrDefault(ocp => ocp.JobType == jobType);
	}
}
