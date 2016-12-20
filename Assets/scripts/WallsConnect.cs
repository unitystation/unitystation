using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction {
    UpperRight = 0,
    LowerRight = 1,
    LowerLeft = 2,
    UpperLeft = 3,
}

public enum TileType {
    Space, Floor, Wall, Glass, Door
}

[ExecuteInEditMode]
public class WallsConnect : MonoBehaviour {

    public Direction direction;

    [Range(0, 1)]
    public int a,b,c;

    public bool change;

    private int ai = 2;
    private int bi = 3;
    private int ci = -4;
    private Sprite[] sprites;
    private SpriteRenderer spriteRenderer;
    private int dirValue;

    void Start () {
        sprites = Resources.LoadAll<Sprite>("walls/wall");
        spriteRenderer = GetComponent<SpriteRenderer>();

        dirValue = (int) direction;
    }
	
	void Update () {
		if(change) {
            change = false;

            UpdateWall();
        }
	}

    private void UpdateWall() {
        var v = a * ai + b * bi + (a * b * c) * ci;

        switch(v) {
            case 0:
                spriteRenderer.sprite = sprites[(dirValue + 5)];
                break;
            case 1:
                spriteRenderer.sprite = sprites[0];
                break;
            case 2:
                spriteRenderer.sprite = sprites[dirValue + 1];
                break;
            case 3:
                spriteRenderer.sprite = sprites[(dirValue + 1) % 4 + 1];
                break;
            case 5:
                spriteRenderer.sprite = sprites[(dirValue + 9)];
                break;
        }
    }

    public void ChangeA(TileType tileType) {

    }

    public void ChangeB(TileType tileType) {

    }

    public void ChangeC(TileType tileType) {

    }
}
