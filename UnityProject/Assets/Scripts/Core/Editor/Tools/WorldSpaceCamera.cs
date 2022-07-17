using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEditor;

public class WorldSpaceCamera : EditorWindow
{
	[SerializeField]
	private GameObject SnapshotCam;

	private string path = "";

	private Vector2Int Bottomleft;
	private Vector2Int Dimensions;

	[MenuItem("Mapping/Open Snapshot Menu")]
	public static void OpenEditor()
	{
		GetWindow<WorldSpaceCamera>(true, "Take WorldSpace Snapshot");
	}

	private void OnGUI()
	{
		Bottomleft = EditorGUILayout.Vector2IntField("Bottom left location:", Bottomleft);
		EditorGUILayout.Space(10);

		Dimensions = EditorGUILayout.Vector2IntField("Dimensions:", Dimensions);
		EditorGUILayout.LabelField("Dimensions are in tiles");

		EditorGUILayout.Space(10);
		EditorGUILayout.LabelField("Image path: " + path);
		if (GUILayout.Button("Browse"))
		{
			path = EditorUtility.OpenFilePanel("Show all images (.png)", "", "png");
		}

		EditorGUILayout.Space(10);
		if(GUILayout.Button("Take Snapshot"))
		{
			TakeSnapshot();
		}
	}

	async Task TakeSnapshot()
	{
		GameObject camObj = Instantiate(SnapshotCam);
		Camera cam = camObj.GetComponent<Camera>();

		camObj.transform.position = new Vector3(Bottomleft.x, Bottomleft.y, 0);

		Texture2D texture = new Texture2D(32 * Dimensions.x, 32 * Dimensions.y, TextureFormat.ARGB32, false);
		Rect rect = new Rect(0, 0, 32, 32);

		texture.filterMode = FilterMode.Point;
		cam.targetTexture = RenderTexture.GetTemporary(32, 32, 16);

		for (int x = 0; x < Dimensions.x; x++)
		{
			camObj.transform.position = new Vector3(camObj.transform.position.x, Bottomleft.y, 0);

			for (int y = 0; y < Dimensions.y; y++)
			{
				cam.Render();
				RenderTexture.active = cam.targetTexture;

				texture.ReadPixels(rect, x * 32, y * 32);

				camObj.transform.position += new Vector3(0, 1, 0);
			}
			camObj.transform.position += new Vector3(1, 0, 0);
		}

		texture.Apply();
		
		byte[] byteArray = texture.EncodeToPNG();
		System.IO.File.WriteAllBytes(path, byteArray);

		DestroyImmediate(camObj);

	}
}
