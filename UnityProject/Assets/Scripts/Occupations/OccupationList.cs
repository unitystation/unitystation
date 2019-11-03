using System.Linq;
using UnityEngine;


/// <summary>
/// Provides a list of allowed occupations.
/// </summary>
[CreateAssetMenu(fileName = "Occupation", menuName = "Occupation/OccupationList", order = 2)]
public class OccupationList : ScriptableObject
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
