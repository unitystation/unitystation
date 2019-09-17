//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEditor;
//using System.Linq;

//[ExecuteInEditMode]
//public class TestSpriteGrabber : MonoBehaviour
//{
//	public List<Sprite> SP;
//	public Sprite RA;
//	private Sprite _RA;
//	// Start is called before the first frame update
//	void Start()
//	{

//	}

//	// Update is called once per frame
//	void Update()
//	{
//		if (RA != _RA)
//		{
//			var tt = AssetDatabase.GetAssetPath(RA).Substring(17);
//			Logger.Log(tt);
//			Logger.Log(tt.Remove(tt.Length - 4));
//			Sprite[] spriteSheetSprites = Resources.LoadAll<Sprite>(tt.Remove(tt.Length - 4)); //To remove the "Assets/Resources/"
//			Logger.Log(spriteSheetSprites.Length.ToString());
//			SP = spriteSheetSprites.ToList();
//		}
//	}
//}
