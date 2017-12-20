using Sprites;
using UnityEngine;

public class FireTile : MonoBehaviour
{
	private readonly float changeRate = 0.1f;
	public GameObject ambientTile;
	private float animSpriteTime;
	private bool burning;

	private float fuel;

	//Not networked, this is client side effects
	public SpriteRenderer spriteRend;

	private Sprite[] sprites;


	public void StartFire(float addFuel)
	{
		if (sprites == null)
		{
			sprites = SpriteManager.FireSprites["fire"];
		}
		fuel += addFuel;
		animSpriteTime = 0f;
		spriteRend.sprite = sprites[Random.Range(0, 100)];
		burning = true;
	}

	public void AddMorefuel(float addFuel)
	{
		fuel += addFuel;
	}

	// Update is called once per frame
	private void Update()
	{
		if (burning)
		{
			animSpriteTime += Time.deltaTime;
			if (animSpriteTime > changeRate)
			{
				animSpriteTime = 0f;
				fuel -= changeRate;
				spriteRend.sprite = sprites[Random.Range(0, 135)];
			}

			if (fuel <= 0f)
			{
				burning = false;
				BurntOut();
			}
		}
	}

	//Ran out of fuel, return to pool
	private void BurntOut()
	{
		// TODO burn tiles
		//		FloorTile fT = MatrixOld.Matrix.At((Vector2)transform.position).GetFloorTile();
		//		if (fT != null) {
		//			fT.AddFireScorch();
		//		}
		PoolManager.Instance.PoolClientDestroy(gameObject);
	}
}