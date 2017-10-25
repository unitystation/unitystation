using UnityEditor;

public class JsonToTilemap
{
    [MenuItem("Tools/Import map (JSON)")]
    static void Json2Map()
    {
        /* create "%stashunName%" parent GO with Grid component
         * create n(layer amount) child "%layerName%" GOs with Tilemap and TilemapRenderer components
         * tilemap.SetTiles( V3IntPositions[], TileBases[] )
         */
    }
}