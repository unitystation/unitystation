using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;


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
	private Department[] departments;
	public IEnumerable<Department> Departments => departments;
}
