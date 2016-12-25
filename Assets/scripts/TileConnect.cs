using Sprites;
using UnityEngine;
using UnityEngine.Events;

public enum SpritePosition {
    UpperRight = 0, LowerRight = 1, LowerLeft = 2, UpperLeft = 3
}

[ExecuteInEditMode]
public class TileConnect: MonoBehaviour {

    public SpritePosition spritePosition;
    public TileType TileType { set; get; } 

    private SpritePosition currentSpritePosition;

    private int[] c = new int[3];

    private Sprite[] sprites;
    private SpriteRenderer spriteRenderer;

    private int[] offsets = { 0, 1, 1, 1, 0, -1, -1, -1 };
    private int[,] adjacentTiles = new int[3, 2];
    
    private int[] matrixPosition = { -1, -1 };
    private int offsetIndex;

    private UnityAction<TileType>[] listeners = new UnityAction<TileType>[3];

    void Awake() {
        sprites = SpriteManager.WallSprites["wall"];
        spriteRenderer = GetComponent<SpriteRenderer>();
        offsetIndex = (int) spritePosition * 2;
    }

    void LateUpdate() {
        if(!Application.isPlaying && spritePosition != currentSpritePosition) {
            currentSpritePosition = spritePosition;
            offsetIndex = (int) spritePosition * 2;
            UpdateListeners();
        }
    }

    public void ChangeParameter(int index, TileType tileType) {
        c[index] = tileType == TileType ? 1 : 0;
        UpdateSprite();
    }

    public void UpdatePosition(int x, int y) {
        matrixPosition[0] = x;
        matrixPosition[1] = y;
        
        UpdateListeners();
        CheckAdjacentTiles();
    }

    private void UpdateListeners() {
        for(int i = 0; i < 3; i++) {
            if(listeners[i] != null) {
                WallMap.RemoveListener(adjacentTiles[i, 0], adjacentTiles[i, 1], listeners[i]);
            } else {
                int i2 = i;
                listeners[i] = new UnityAction<TileType>(x => ChangeParameter(i2, x));
            }
            adjacentTiles[i, 0] = matrixPosition[0] + offsets[(offsetIndex + i) % 8];
            adjacentTiles[i, 1] = matrixPosition[1] + offsets[(offsetIndex + i + 2) % 8];
            
            WallMap.AddListener(adjacentTiles[i, 0], adjacentTiles[i, 1], listeners[i]);
        }
    }

    private void CheckAdjacentTiles() {
        for(int i = 0; i < 3; i++) {
            ChangeParameter(i, WallMap.GetTypeAt(adjacentTiles[i, 0], adjacentTiles[i, 1]));
        }
    }

    private void UpdateSprite() {
        int index;

        switch(c[0] * 3 + c[2] * 2 + (c[0] * c[1] * c[2]) * -4) {
            case 0: index = (int) spritePosition + 5; break;
            case 2: index = (int) spritePosition + 1; break;
            case 3: index = ((int) spritePosition + 1) % 4 + 1; break;
            case 5: index = ((int) spritePosition + 9); break;
            default: index = 0; break;
        }

        spriteRenderer.sprite = sprites[index];
    }
}