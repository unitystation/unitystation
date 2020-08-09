using System.Collections.Generic;
using System.Linq;
using ScriptableObjects;
using UnityEngine;


/// <summary>
/// Singleton. Provides a list of currently enabled departments and the order in which they
/// should appear in the job preferences.
/// </summary>
[CreateAssetMenu(fileName = "DepartmentListSingleton", menuName = "Singleton/DepartmentList")]
public class DepartmentList : SingletonScriptableObject<DepartmentList>
{
	[SerializeField]
	[Tooltip("Allowed departments, and the order in which they should be displayed in" +
	         " job preferences.")]
	private Department[] departments = null;
	public IEnumerable<Department> Departments => departments;

	/// <summary>
	/// Returns all head jobs defined in each Department ScriptableObject
	/// </summary>
	public IEnumerable<Occupation> GetAllHeadJobs()
	{
		return departments.SelectMany(dept => dept.HeadOccupations);
	}

	/// <summary>
	/// Returns all non-head jobs defined in each Department ScriptableObject
	/// </summary>
	public IEnumerable<Occupation> GetAllNormalJobs()
	{
		return departments.SelectMany(dept => dept.Occupations);
	}
}
