
using UnityEngine;
/// <summary>
/// Singleton inside Managers.prefab that you can access to get camera used for snapshots
/// </summary>
[RequireComponent(typeof(Camera))]
public class SnapshotCamera : MonoBehaviour
{
	private Camera cam;
	public Camera Camera => cam ? cam : cam = GetComponent<Camera>();

	public static SnapshotCamera Instance;

	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(this);
		}
	}
}