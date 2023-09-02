
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UI;

public class UpdateTextures : MonoBehaviour
{
	public Sprite TestisSprite;

	public Texture2D[] inbuiltTextures; // Assign your inbuilt textures here in the inspector
	public Texture2D[] customTextures; // Assign your duplicated textures here in the inspector

	private bool changesMade = false;
	[NaughtyAttributes.Button]
	public void DO()
	{
        // Create a dictionary to map inbuilt textures to new textures
        Dictionary<Texture2D, Texture2D> textureMap = new Dictionary<Texture2D, Texture2D>();

        // Fill the dictionary with inbuilt and custom textures
        for (int i = 0; i < inbuiltTextures.Length; i++)
        {
            if (inbuiltTextures[i] != null && customTextures.Length > i)
            {
                textureMap[inbuiltTextures[i]] = customTextures[i];
            }
        }


        // Update Sprites used by components (e.g., Image)
        Component[] componentsWithSprite = Resources.FindObjectsOfTypeAll<Component>();
        foreach (Component component in componentsWithSprite)
        {
	        Image image = component.GetComponent<Image>();
	        if (image != null && image.sprite != null )
	        {

		        if (textureMap.ContainsKey(image.sprite.texture))
		        {
			        // Find the existing sprite used by the Image component
			        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
			        foreach (Sprite sprite in sprites)
			        {
				        if (sprite.texture == textureMap[image.sprite.texture])
				        {
					        image.sprite = sprite;
					        Undo.RecordObject(image, "Set Sprite");
					        EditorUtility.SetDirty(image);
					        changesMade = true;
					        break;
				        }
			        }
		        }
	        }

	        var images = component.GetComponentsInChildren<Image>();
	        foreach (var imageA in images)
	        {
		        if (imageA != null && imageA.sprite != null )
		        {
			        if (textureMap.ContainsKey(imageA.sprite.texture))
			        {
				        // Find the existing sprite used by the Image component
				        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
				        foreach (Sprite sprite in sprites)
				        {
					        if (sprite.texture == textureMap[imageA.sprite.texture])
					        {
						        imageA.sprite = sprite;
						        Undo.RecordObject(imageA, "Set Sprite");
						        EditorUtility.SetDirty(imageA);
						        changesMade = true;
						        break;
					        }
				        }
			        }
		        }
	        }
        }

        // Save the changes if any were made
        if (changesMade)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
