
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
	public Texture2D InbuiltBackground;
	public Texture2D customBackground;

	public Texture2D InbuiltCheckmark;
	public Texture2D customCheckmark;

	public Texture2D InbuiltDropDown;
	public Texture2D customDropDown;

	public Texture2D InbuiltInputField;
	public Texture2D customInputField;

	public Texture2D InbuiltKnob;
	public Texture2D customKnob;

	public Texture2D InbuiltUImask;
	public Texture2D customUImask;

	public Texture2D InbuiltUISprite;
	public Texture2D customUISprite;

	private  List<Texture2D> inbuiltTextures = new List<Texture2D>();
	private List<Texture2D> customTextures = new List<Texture2D>();

	private bool changesMade = false;
	[NaughtyAttributes.Button]
	public void DO()
	{
		inbuiltTextures.Add(InbuiltBackground);
		customTextures.Add(customBackground);


		inbuiltTextures.Add(InbuiltCheckmark);
		customTextures.Add(customCheckmark);

		inbuiltTextures.Add(InbuiltDropDown);
		customTextures.Add(customDropDown);


		inbuiltTextures.Add(InbuiltInputField);
		customTextures.Add(customInputField);

		inbuiltTextures.Add(InbuiltKnob);
		customTextures.Add(customKnob);

		inbuiltTextures.Add(InbuiltUImask);
		customTextures.Add(customUImask);

		inbuiltTextures.Add(InbuiltUISprite);
		customTextures.Add(customUISprite);

        // Create a dictionary to map inbuilt textures to new textures
        Dictionary<Texture2D, Texture2D> textureMap = new Dictionary<Texture2D, Texture2D>();

        // Fill the dictionary with inbuilt and custom textures
        for (int i = 0; i < inbuiltTextures.Count; i++)
        {
            if (inbuiltTextures[i] != null && customTextures.Count > i)
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
