using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Light2D;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Light2D.Examples
{
    public class MapGenerator : MonoBehaviour
    {
        private struct CollMapPoint
        {
            public float Noise;
            public BlockSetProfile.BlockInfo BlockInfo;
            public BlockSetProfile.BlockType BlockType;

            public CollMapPoint(float noise)
            {
                Noise = noise;
                BlockInfo = null;
                BlockType = BlockSetProfile.BlockType.Empty;
            }
        }

        public BlockSetProfile BlockSet;
        public bool RandomSeed;
        public int Seed;

        public GameObject MeshObjectPrefab;
        public GameObject LightObstaclesPrefab;
        public GameObject AmbientLightPrefab;
        public Transform Container;
        private Camera _mainCamera;
        private float _randFirstAddX;
        private float _randFirstAddY;
        private float _randSecondAddX;
        private float _randSecondAddY;
        private const int ChunkSize = 32;
        private Dictionary<Point2, GameObject> _tiles = new Dictionary<Point2, GameObject>();
        private Dictionary<Point2, CollMapPoint> _collMapPoints = new Dictionary<Point2, CollMapPoint>();
        private List<Vector3> _vertices = new List<Vector3>();
        private List<Vector2> _uvs = new List<Vector2>();
        private List<int> _triangles = new List<int>();
        private List<Color> _lightAbsorptionColors = new List<Color>();
        private List<Color> _lightEmissionColors = new List<Color>();
        private List<Color> _colors = new List<Color>();

        private void Awake()
        {
            _mainCamera = Camera.main;

            if (RandomSeed)
            {
                Point2 spawnPoint = Point2.Round(FindObjectOfType<Spacecraft>().MainRigidbody.position);
                do
                {
                    Seed = Random.Range(-9999999, 9999999);
                    InitRandom();
                    _collMapPoints.Clear();
                } while (!IsGoodSpawnPoint(spawnPoint));
            }
            else
            {
                InitRandom();
            }
        }

        void InitRandom()
        {
            var rand = new SimpleRNG(Seed);
            _randFirstAddX = (rand.value - 0.5f) * 1000f;
            _randFirstAddY = (rand.value - 0.5f) * 10000;
            _randSecondAddX = (rand.value - 0.5f) * 1000f;
            _randSecondAddY = (rand.value - 0.5f) * 1000f;
        }

        private void Start()
        {
            foreach (var transf in Container.Cast<Transform>().ToArray())
            {
                Util.Destroy(transf.gameObject);
            }
            StartCoroutine(ChunkCreatorCoroutine());
            StartCoroutine(ChunkDestroyerCoroutine());
        }

        private void Update()
        {
        }

        private IEnumerator ChunkCreatorCoroutine()
        {
            bool firstRun = true;
            while (true)
            {
                var camPos = _mainCamera.transform.position;
                var camChunk = Point2.Round(camPos.x/ChunkSize, camPos.y/ChunkSize);
                var camHalfHeight = Mathf.CeilToInt(_mainCamera.orthographicSize/ChunkSize) + 2;
                var camHalfWidth = Mathf.CeilToInt(_mainCamera.orthographicSize*_mainCamera.aspect/ChunkSize) + 2;

                for (int x = camChunk.x - camHalfWidth; x <= camChunk.x + camHalfWidth; x++)
                {
                    for (int y = camChunk.y - camHalfHeight; y <= camChunk.y + camHalfHeight; y++)
                    {
                        camPos = _mainCamera.transform.position;
                        camChunk = Point2.Round(camPos.x/ChunkSize, camPos.y/ChunkSize);

                        if (_tiles.ContainsKey(new Point2(x, y)))
                            continue;

                        var chunk = GenerateChunk(x, y);
                        _tiles[new Point2(x, y)] = chunk;

                        if (!firstRun)
                            yield return null;
                    }
                    yield return null;
                }
                firstRun = false;
            }
        }

        private IEnumerator ChunkDestroyerCoroutine()
        {
            var removedChunks = new List<Point2>();
            while (true)
            {
                removedChunks.Clear();
                foreach (var chunk in _tiles)
                {
                    var camPos = _mainCamera.transform.position;
                    var camChunk = Point2.Round(camPos.x/ChunkSize, camPos.y/ChunkSize);
                    var camHalfHeight = Mathf.CeilToInt(_mainCamera.orthographicSize/ChunkSize) + 20;
                    var camHalfWidth = Mathf.CeilToInt(_mainCamera.orthographicSize*_mainCamera.aspect/ChunkSize) + 20;

                    var pos = chunk.Key;
                    if (pos.x < camChunk.x - camHalfWidth || pos.x > camChunk.x + camHalfWidth ||
                        pos.y < camChunk.y - camHalfHeight || pos.y > camChunk.y + camHalfHeight)
                    {
                        removedChunks.Add(pos);
                    }
                }

                yield return null;

                foreach (var pos in removedChunks)
                {
                    var camPos = _mainCamera.transform.position;
                    var camChunk = Point2.Round(camPos.x/ChunkSize, camPos.y/ChunkSize);
                    var camHalfHeight = Mathf.CeilToInt(_mainCamera.orthographicSize/ChunkSize) + 2;
                    var camHalfWidth = Mathf.CeilToInt(_mainCamera.orthographicSize*_mainCamera.aspect/ChunkSize) + 2;

                    var chunk = _tiles[pos];
                    if (pos.x < camChunk.x - camHalfWidth || pos.x > camChunk.x + camHalfWidth ||
                        pos.y < camChunk.y - camHalfHeight || pos.y > camChunk.y + camHalfHeight)
                    {
                        Destroy(chunk);
                        _tiles.Remove(pos);
                        yield return null;
                    }
                }

                yield return null;
            }
        }

        private GameObject GenerateChunk(int chunkX, int chunkY)
        {
            return GenerateBlocksJoined(chunkX*ChunkSize, chunkY*ChunkSize);
        }

        private GameObject GenerateBlocksJoined(int xOffest, int yOffest)
        {
            var rand = new SimpleRNG(Util.Hash(Seed, xOffest, yOffest));

            _vertices.Clear();
            _uvs.Clear();
            _triangles.Clear();
            _lightAbsorptionColors.Clear();
            _lightEmissionColors.Clear();

            var meshObj = (GameObject) Instantiate(MeshObjectPrefab);
            meshObj.name = "Block Mesh { X = " + xOffest/ChunkSize + "; Y = " + yOffest/ChunkSize + " }";
            var meshObjTransform = meshObj.transform;
            meshObjTransform.position = meshObjTransform.position.WithXY(xOffest, yOffest);
            meshObjTransform.parent = Container;

            var collMap = new CollMapPoint[ChunkSize, ChunkSize];

            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    collMap[x, y] = GetCollMapPoint(x + xOffest, y + yOffest);
                }
            }

            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    var blockInfo = collMap[x, y].BlockInfo;

                    if (blockInfo.AditionalObjectPrefab != null && blockInfo.AditionalObjectProbability >= rand.value)
                    {
                        var addObj = (GameObject) Instantiate(blockInfo.AditionalObjectPrefab);

                        addObj.transform.parent = meshObjTransform;
                        addObj.transform.localPosition = blockInfo.AditionalObjectPrefab
                            .transform.position.WithXY(x + 0.5f, y + 0.5f);
                    }

                    if (blockInfo.SpriteInfo.Length == 0)
                    {
                        Debug.LogError("Sprite Info is broken");
                        continue;
                    }

                    var compactInfo =
                        (SafeIndex(collMap, x, y + 1, ChunkSize, ChunkSize,
                            () => GetCollMapPoint(x + xOffest, y + yOffest))
                            .BlockType == BlockSetProfile.BlockType.CollidingWall
                            ? 1
                            : 0) +
                        (SafeIndex(collMap, x + 1, y, ChunkSize, ChunkSize,
                            () => GetCollMapPoint(x + xOffest, y + yOffest))
                            .BlockType == BlockSetProfile.BlockType.CollidingWall
                            ? 2
                            : 0) +
                        (SafeIndex(collMap, x, y - 1, ChunkSize, ChunkSize,
                            () => GetCollMapPoint(x + xOffest, y + yOffest))
                            .BlockType == BlockSetProfile.BlockType.CollidingWall
                            ? 4
                            : 0) +
                        (SafeIndex(collMap, x - 1, y, ChunkSize, ChunkSize,
                            () => GetCollMapPoint(x + xOffest, y + yOffest))
                            .BlockType == BlockSetProfile.BlockType.CollidingWall
                            ? 8
                            : 0);

                    CreatePoint(x, y, blockInfo, compactInfo, false, rand);
                }
            }

            var blockMesh = new Mesh();
            blockMesh.vertices = _vertices.ToArray();
            blockMesh.uv = _uvs.ToArray();
            blockMesh.triangles = _triangles.ToArray();
            blockMesh.RecalculateBounds();

            var meshFilter = meshObj.GetComponent<MeshFilter>();
            meshFilter.mesh = blockMesh;

            var meshRenderer = meshObj.GetComponent<MeshRenderer>();
            var texture = BlockSet.BlockInfos
                .First(bi => bi.SpriteInfo.Any(si => si != null))
                .SpriteInfo.First(ti => ti != null)
                .texture;
            var mpb = new MaterialPropertyBlock();
            mpb.SetTexture("_MainTex", texture);
            meshRenderer.SetPropertyBlock(mpb);

            for (int x = 0; x < ChunkSize; x++)
            {
                var yStart = 0;
                for (int y = 0; y < ChunkSize; y++)
                {
                    if (collMap[x, y].BlockInfo.BlockType != BlockSetProfile.BlockType.CollidingWall)
                    {
                        if (y - yStart > 0)
                        {
                            var obj = new GameObject();
                            obj.layer = meshObj.layer;
                            obj.transform.parent = meshObjTransform;
                            obj.transform.localPosition = new Vector3(x, 0);
                            obj.name = "Collider x = " + x;
                            var coll = obj.AddComponent<BoxCollider2D>();
                            coll.size = new Vector2(1, (y - yStart));
                            coll.offset = new Vector2(0.5f, yStart + coll.size.y/2f);
                        }
                        yStart = y + 1;
                    }
                }
                if (ChunkSize - yStart > 0)
                {
                    var obj = new GameObject();
                    obj.layer = meshObj.layer;
                    obj.transform.parent = meshObjTransform;
                    obj.transform.localPosition = new Vector3(x, 0);
                    obj.name = "Collider x = " + x;
                    var coll = obj.AddComponent<BoxCollider2D>();
                    coll.size = new Vector2(1, (ChunkSize - yStart));
                    coll.offset = new Vector2(0.5f, yStart + coll.size.y/2f);
                }
            }

            var lightObstaclesObject = (GameObject) Instantiate(LightObstaclesPrefab);
            lightObstaclesObject.transform.parent = meshObjTransform;
            lightObstaclesObject.transform.localPosition = Vector3.zero;
            //lightObstaclesObject.transform.localPosition += new Vector3(0, 0, -10);
            var lightObstaclesMeshFilter = lightObstaclesObject.GetComponent<MeshFilter>();
            lightObstaclesMeshFilter.mesh = ChunkMeshFromColors(_lightAbsorptionColors);

            var ambientLightObject = (GameObject) Instantiate(AmbientLightPrefab);
            ambientLightObject.transform.parent = meshObjTransform;
            ambientLightObject.transform.localPosition = Vector3.zero;
            //ambientLightObject.transform.localPosition += new Vector3(0, 0, -5);
            var ambientLightMeshFilter = ambientLightObject.GetComponent<MeshFilter>();
            ambientLightMeshFilter.mesh = ChunkMeshFromColors(_lightEmissionColors);

            return meshObj;
        }

        private Mesh ChunkMeshFromColors(List<Color> colors)
        {
            _vertices.Clear();
            _triangles.Clear();
            _colors.Clear();

            const float add = 0;

            for (int x = 0; x < ChunkSize; x++)
            {
                var yStart = 0;
                var startC = colors[x*ChunkSize];
                for (int y = 0; y < ChunkSize; y++)
                {
                    var currC = colors[y + x*ChunkSize];
                    if (currC.r != startC.r || currC.g != startC.g || currC.b != startC.b || currC.a != startC.a)
                    {
                        var startVert = _vertices.Count;

                        _vertices.Add(new Vector3(x - add, yStart - add, 0));
                        _vertices.Add(new Vector3(x + 1 + add, yStart - add, 0));
                        _vertices.Add(new Vector3(x - add, y + add, 0));
                        _vertices.Add(new Vector3(x + 1 + add, y + add, 0));

                        _triangles.Add(startVert + 2);
                        _triangles.Add(startVert + 1);
                        _triangles.Add(startVert);
                        _triangles.Add(startVert + 1);
                        _triangles.Add(startVert + 2);
                        _triangles.Add(startVert + 3);

                        for (int i = 0; i < 4; i++)
                            _colors.Add(startC);

                        startC = currC;

                        yStart = y;
                    }
                }
                if (ChunkSize - yStart > 0)
                {
                    var startVert = _vertices.Count;

                    _vertices.Add(new Vector3(x - add, yStart - add, 0));
                    _vertices.Add(new Vector3(x + 1 + add, yStart - add, 0));
                    _vertices.Add(new Vector3(x - add, ChunkSize + add, 0));
                    _vertices.Add(new Vector3(x + 1 + add, ChunkSize + add, 0));

                    _triangles.Add(startVert + 2);
                    _triangles.Add(startVert + 1);
                    _triangles.Add(startVert);
                    _triangles.Add(startVert + 1);
                    _triangles.Add(startVert + 2);
                    _triangles.Add(startVert + 3);

                    for (int i = 0; i < 4; i++)
                        _colors.Add(startC);
                }
            }

            var mesh = new Mesh();
            mesh.vertices = _vertices.ToArray();
            mesh.triangles = _triangles.ToArray();
            mesh.colors = _colors.ToArray();

            return mesh;
        }

        private void CreatePoint(int x, int y, BlockSetProfile.BlockInfo blockInfo, int compactInfo,
            bool noLightEffects, SimpleRNG rand, BlockSetProfile.BlockType? isColliding = null)
        {
            var sprite = blockInfo.SpriteInfo
                .RandomElement(ti => 1, rand);

            //if (tilingInfo == null)
            //{
            //    Debug.LogError("Tiling info not found");
            //    return;
            //}

            //var sprite = tilingInfo.Sprite;

            if (sprite == null)
            {
                Debug.LogError("Tiling info is broken");
                return;
            }

            CreatePoint(x, y, sprite, blockInfo, isColliding == null ? blockInfo.BlockType : isColliding.Value,
                noLightEffects);
        }

        private void CreatePoint(int x, int y, Sprite sprite, BlockSetProfile.BlockInfo blockInfo,
            BlockSetProfile.BlockType isColliding, bool noLightEffect)
        {
            var textureSize = new Vector2(sprite.texture.width, sprite.texture.height);
            var uvRect = sprite.textureRect;

            var startVert = _vertices.Count;
            const float add = 0.01f;
            float z = isColliding == BlockSetProfile.BlockType.CollidingWall ? 0 : 5;
            _vertices.Add(new Vector3(x - add, y - add, z));
            _vertices.Add(new Vector3(x + 1 + add, y - add, z));
            _vertices.Add(new Vector3(x - add, y + 1 + add, z));
            _vertices.Add(new Vector3(x + 1 + add, y + 1 + add, z));

            _uvs.Add(new Vector2(uvRect.xMin/textureSize.x, uvRect.yMin/textureSize.y)); // 0, 0
            _uvs.Add(new Vector2(uvRect.xMax/textureSize.x, uvRect.yMin/textureSize.y)); // 1, 0
            _uvs.Add(new Vector2(uvRect.xMin/textureSize.x, uvRect.yMax/textureSize.y)); // 0, 1
            _uvs.Add(new Vector2(uvRect.xMax/textureSize.x, uvRect.yMax/textureSize.y)); // 1, 1

            _triangles.Add(startVert + 2);
            _triangles.Add(startVert + 1);
            _triangles.Add(startVert);
            _triangles.Add(startVert + 1);
            _triangles.Add(startVert + 2);
            _triangles.Add(startVert + 3);

            //for (int i = 0; i < 4; i++)
            //{
            _lightAbsorptionColors.Add(noLightEffect ? new Color() : blockInfo.LightAbsorption);
            _lightEmissionColors.Add(noLightEffect ? new Color() : blockInfo.LightEmission);
            //}
        }

        private CollMapPoint GetCollMapPoint(int x, int y, BlockSetProfile.BlockType? colliding = null)
        {
            CollMapPoint cachedPoint;
            if (_collMapPoints.TryGetValue(new Point2(x, y), out cachedPoint) &&
                (colliding == null || colliding.Value == cachedPoint.BlockType))
            {
                return cachedPoint;
            }

            var noise = GetNoise(x, y);

            var matchingBlockInfos = BlockSet.BlockInfos.FindAll(b =>
                b.MinNoise <= noise && b.MaxNoise >= noise &&
                (colliding == null || colliding.Value == b.BlockType));

            if (matchingBlockInfos.Count == 0 && colliding != null)
            {
                matchingBlockInfos = BlockSet.BlockInfos
                    .FindAll(b => colliding.Value == b.BlockType);
            }

            if (matchingBlockInfos.Count == 0)
            {
                Debug.LogError("No matching blocks found");
                return default(CollMapPoint);
            }

            var rand = new SimpleRNG(Util.Hash(Seed, x, y));
            var blockInfo = matchingBlockInfos
                .RandomElement(bi => bi.Weight, rand);

            var point = new CollMapPoint(noise) {BlockInfo = blockInfo, BlockType = blockInfo.BlockType};
            _collMapPoints[new Point2(x, y)] = point;
            return point;
        }

        float GetNoise(int x, int y)
        {
            var noise1 = (Noise.Generate(
                x * BlockSet.FirstNoiseScale + _randFirstAddX,
                y * BlockSet.FirstNoiseScale + _randFirstAddY) - 0.5f) * 2f;
            var noise2 = (Noise.Generate(
                x * BlockSet.SecondNoiseScale + _randSecondAddX,
                y * BlockSet.SecondNoiseScale + _randSecondAddY) - 0.5f) * 2f;

            var noise = Mathf.Clamp01((noise1 + noise2 * BlockSet.SecondNoiseMul) / 2f + 0.5f);

            return noise;
        }

        bool IsGoodSpawnPoint(Point2 point)
        {
            for (int x = point.x - 10; x <= point.x + 10; x++)
            {
                for (int y = point.y - 10; y < point.y + 10; y++)
                {
                    if (GetCollMapPoint(x, y).BlockType == BlockSetProfile.BlockType.CollidingWall)
                        return false;
                }
            }
            return true;
        }

        [ContextMenu("Recreate map")]
        private void RecreateMap()
        {
            foreach (var tile in _tiles)
            {
                Destroy(tile.Value);
            }
            _tiles.Clear();
        }

        private T SafeIndex<T>(T[,] arr, int x, int y, int w, int h, Func<T> noneFunc)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return noneFunc();
            return arr[x, y];
        }
    }
}