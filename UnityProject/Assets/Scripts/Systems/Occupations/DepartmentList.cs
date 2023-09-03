using System.Collections.Generic;
using System.Linq;
using Logs;
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
	private  List<Department> departments = new List<Department>();
	public IEnumerable<Department> Departments => departments;

	/// <summary>
	/// Returns all head jobs defined in each Department ScriptableObject
	/// </summary>
	public IEnumerable<Occupation> GetAllHeadJobs()
	{
		// Log Errors for missing head jobs (Improves debugging)
		foreach (Department dept in departments.Where(p => p.HeadOccupations == null))
			Loggy.LogError($"Missing head of department reference for department {dept.Description}", Category.Jobs);

		// Won't crash if a department is missing it's headOccupation reference.
		return departments.Where(p => p.HeadOccupations != null).SelectMany(dept => dept.HeadOccupations);
	}

	/// <summary>
	/// Returns all non-head jobs defined in each Department ScriptableObject
	/// </summary>
	public IEnumerable<Occupation> GetAllNormalJobs()
	{
		return departments.SelectMany(dept => dept.Occupations);
	}
}
