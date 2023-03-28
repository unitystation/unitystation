using UnityEngine;

namespace _3D
{
	public class SetCubeSprite : MonoBehaviour
	{

		public MeshRenderer MeshRenderer;

		public void SetSprite(Sprite Sprite)
		{


// Create a new Texture2D object with the dimensions of the sprite
			Texture2D myTexture = new Texture2D((int)Sprite.rect.width, (int)Sprite.rect.height);

// Get the pixels from the sprite's texture
			Color[] spritePixels = Sprite.texture.GetPixels(
				(int)Sprite.textureRect.x,
				(int)Sprite.textureRect.y,
				(int)Sprite.textureRect.width,
				(int)Sprite.textureRect.height
			);

// Set the pixels in the Texture2D object
			myTexture.SetPixels(spritePixels);

// Apply the changes to the texture
			myTexture.Apply();

// Now you can use the generated texture as needed

// Set the filter mode to point to avoid the fuzzy scaling of the texture
			myTexture.filterMode = FilterMode.Point;

// Set the texture type to sprite if you want to use the texture as a sprite
// Note: the texture type can only be set before the texture is uploaded to the GPU, so it
// must be set before calling Texture2D.Apply().
			myTexture.wrapMode = TextureWrapMode.Clamp;

// Create a new material instance for this cube
			Material cubeMaterial = new Material(MeshRenderer.material);

			// Set the new texture on the material instance
			cubeMaterial.SetTexture("_MainTex", myTexture);

			// Apply the new material instance to this cube's renderer
			MeshRenderer.material = cubeMaterial;
		}

	}
}