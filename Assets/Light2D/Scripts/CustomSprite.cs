using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Light2D;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Light2D
{
    /// <summary>
    /// Custom sprite wich uses MeshFilter and MeshRenderer to render.
    /// Main improvement from Unity SpriteRenderer is that you can access and modify mesh.
    /// Also multiple CustomSprites could be merged to single mesh with MeshCombiner,
    /// which gives much better performance for small meshes than StaticBatchingUtility.Combine.
    /// </summary>
    [ExecuteInEditMode]
    public class CustomSprite : MonoBehaviour
    {
        /// <summary>
        /// Vertex color of mesh.
        /// </summary>
        public Color Color = Color.white;

        /// <summary>
        /// Sprite from which mesh will be generated.
        /// </summary>
        public Sprite Sprite;

        /// <summary>
        /// Sorting order of MeshRenderer.
        /// </summary>
        public int SortingOrder;

        /// <summary>
        /// Material to be used.
        /// </summary>
        public Material Material;

        // mesh data
        protected Color[] _colors;
        protected Vector2[] _uv0;
        protected Vector2[] _uv1;
        protected Vector3[] _vertices;
        protected int[] _triangles;
        protected Vector4[] _tangents;

        protected bool _isMeshDirty = false;
        protected MeshRenderer _meshRenderer;
        protected MeshFilter _meshFilter;
        protected Mesh _mesh;
        private Color _oldColor;
        private Sprite _oldSprite;
        private Material _oldMaterial;
        private MaterialKey _oldMaterialKey;
        public static Dictionary<MaterialKey, MaterialValue> MaterialMap = new Dictionary<MaterialKey, MaterialValue>();
        private const string GeneratedMaterialName = "Generated Material (DONT change it)";
        private const string GeneratedMeshName = "Generated Mesh (DONT change it)";

        public bool RendererEnabled { get; private set; }

        /// <summary>
        /// Is that sprite is staticaly batched?
        /// </summary>
        public bool IsPartOfStaticBatch
        {
            get { return _meshRenderer.isPartOfStaticBatch; }
        }

        protected virtual void OnEnable()
        {
            _colors = new Color[4];
            _uv1 = new Vector2[4];
            _uv0 = new Vector2[4];
            _vertices = new Vector3[4];
            _triangles = new[] {2, 1, 0, 1, 2, 3};
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();

            if (_meshRenderer == null)
            {
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
                _meshRenderer.castShadows = _meshRenderer.receiveShadows = false;
            }

            if (_meshFilter == null)
            {
                _meshFilter = gameObject.AddComponent<MeshFilter>();
            }

#if UNITY_EDITOR
            if (Material == null)
            {
                Material = Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
            }
#endif

            TryReleaseMesh();
            _meshFilter.sharedMesh = _mesh = new Mesh();
            _mesh.MarkDynamic();
            _mesh.name = GeneratedMeshName;

            _tangents = new Vector4[4];
            for (int i = 0; i < _tangents.Length; i++)
                _tangents[i] = new Vector4(1, 0, 0);

            UpdateMeshData(true);

            RendererEnabled = _meshRenderer.enabled;
        }

        protected virtual void Start()
        {
            UpdateMeshData(true);
        }

        private void OnWillRenderObject()
        {
            UpdateMeshData();
            if (Application.isPlaying && LightingSystem.Instance.EnableNormalMapping)
            {
                RendererEnabled = _meshRenderer.enabled;
                _meshRenderer.enabled = false; 
            }
        }

        private void OnRenderObject()
        {
            if (Application.isPlaying && LightingSystem.Instance.EnableNormalMapping)
            {
                _meshRenderer.enabled = RendererEnabled;
            }
        }

        /// <summary>
        /// Getting material from cache or instantiating new one.
        /// </summary>
        /// <returns></returns>
        public Material GetOrCreateMaterial()
        {
            TryReleaseMaterial();

            if (Material == null || Sprite == null)
                return null;

            MaterialValue matValue;
            var key = new MaterialKey(Material, Sprite.texture);

            if (!MaterialMap.TryGetValue(key, out matValue))
            {
                var mat = (Material)Instantiate(Material);
                mat.name = GeneratedMaterialName;
                mat.mainTexture = Sprite.texture;
                MaterialMap[key] = matValue = new MaterialValue(mat, 1);
            }
            else
            {
                matValue.UsageCount++;
            }

            _oldMaterialKey = key;

            return matValue.Material;
        }
        
        /// <summary>
        /// Getting material from cache or instantiating new one.
        /// </summary>
        /// <returns></returns>
        public static Material GetOrCreateMaterial(Material baseMaterial, Texture2D texture, out MaterialKey materialKey)
        {
            if (baseMaterial == null || texture == null)
            {
                materialKey = null;
                return null;
            }

            MaterialValue matValue;
            var key = materialKey = new MaterialKey(baseMaterial, texture);

            if (!MaterialMap.TryGetValue(key, out matValue))
            {
                var mat = (Material)Instantiate(baseMaterial);
                mat.name = GeneratedMaterialName;
                mat.mainTexture = texture;
                MaterialMap[key] = matValue = new MaterialValue(mat, 1);
            }
            else
            {
                matValue.UsageCount++;
            }
            
            return matValue.Material;
        }

        /// <summary>
        /// Deleting material from cache with reference counting.
        /// </summary>
        /// <param name="key"></param>
        public static void ReleaseMaterial(MaterialKey key)
        {
            MaterialValue matValue;

            if (!MaterialMap.TryGetValue(key, out matValue))
                return;

            matValue.UsageCount--;

            if (matValue.UsageCount <= 0)
            {
                Util.Destroy(matValue.Material);
                MaterialMap.Remove(key);
            }
        }

        void TryReleaseMesh()
        {
            if (_meshFilter != null && _meshFilter.sharedMesh != null &&
                _meshFilter.sharedMesh.name == GeneratedMeshName && _mesh == _meshFilter.sharedMesh)
            {
                Util.Destroy(_meshFilter.sharedMesh);
                _meshFilter.sharedMesh = null;
            }
        }

        void TryReleaseMaterial()
        {
            if (_oldMaterialKey != default(MaterialKey))
            {
                ReleaseMaterial(_oldMaterialKey);
                _oldMaterialKey = default(MaterialKey);
            }
        }

        void OnDestroy()
        {
            TryReleaseMesh();
            TryReleaseMaterial();
        }

        protected virtual void UpdateColor()
        {
            for (int i = 0; i < _colors.Length; i++)
                _colors[i] = Color;
        }

        /// <summary>
        /// Recreating mesh data for Sprite based on it's bounds.
        /// </summary>
        protected virtual void UpdateSprite()
        {
            if (Sprite == null)
                return;
            
            var rect = Sprite.textureRect;
            var bounds = Sprite.bounds;
            var tex = Sprite.texture;
            var textureSize = new Point2(tex.width, tex.height);
            
            // HACK: mipmap could cause texture padding sometimes so padded size of texture needs to be computed.
            var realSize =
#if UNITY_EDITOR || UNITY_STANDALONE
                tex.mipmapCount <= 1
#else
                true
#endif
                    ? textureSize
                    : new Point2(Mathf.NextPowerOfTwo(textureSize.x), Mathf.NextPowerOfTwo(textureSize.y));

            var unitSize2 = rect.size/Sprite.pixelsPerUnit/2f;
            var offest = (Vector2) bounds.center;

            _vertices[0] = new Vector3(-unitSize2.x + offest.x, -unitSize2.y + offest.y, 0);
            _vertices[1] = new Vector3(unitSize2.x + offest.x, -unitSize2.y + offest.y, 0);
            _vertices[2] = new Vector3(-unitSize2.x + offest.x, unitSize2.y + offest.y, 0);
            _vertices[3] = new Vector3(unitSize2.x + offest.x, unitSize2.y + offest.y, 0);

            _uv0[0] = new Vector2(rect.xMin/realSize.x, rect.yMin/realSize.y); // 0, 0
            _uv0[1] = new Vector2(rect.xMax/realSize.x, rect.yMin/realSize.y); // 1, 0
            _uv0[2] = new Vector2(rect.xMin/realSize.x, rect.yMax/realSize.y); // 0, 1
            _uv0[3] = new Vector2(rect.xMax/realSize.x, rect.yMax/realSize.y); // 1, 1

            for (int i = 0; i < 4; i++)
            {
                _colors[i] = Color;
            }

            _meshRenderer.sharedMaterial = GetOrCreateMaterial();
        }

        /// <summary>
        /// Clearing and rebuilding mesh.
        /// </summary>
        protected virtual void UpdateMesh()
        {
            _mesh.Clear();
            _mesh.vertices = _vertices;
            _mesh.triangles = _triangles;
            _mesh.uv = _uv0;
            _mesh.uv2 = _uv1;
            _mesh.colors = _colors;
            _mesh.tangents = _tangents;
        }

        /// <summary>
        /// Checking public fields and mesh data, then rebuilding internal state if changes found.
        /// </summary>
        /// <param name="forceUpdate">Force update even if no changes found.</param>
        protected virtual void UpdateMeshData(bool forceUpdate = false)
        {
            if (_meshRenderer == null || _meshFilter == null || IsPartOfStaticBatch)
                return;

            _meshRenderer.sortingOrder = SortingOrder;

            if (Color != _oldColor || forceUpdate)
            {
                UpdateColor();
                _isMeshDirty = true;
                _oldColor = Color;
            }
            if (Sprite != _oldSprite || Material != _oldMaterial || forceUpdate)
            {
                UpdateSprite();
                _isMeshDirty = true;
                _oldSprite = Sprite;
                _oldMaterial = Material;
            }
            if (_isMeshDirty)
            {
                UpdateMesh();
                _isMeshDirty = false;
            }
        }

        /// <summary>
        /// Used as a value to material map to support reference counting.
        /// </summary>
        public class MaterialValue
        {
            /// <summary>
            /// Instantiated material from MaterialKey.Material with texture from MaterialKey.Texture.
            /// </summary>
            public Material Material;

            /// <summary>
            /// Count of CustomSprites using that material.
            /// </summary>
            public int UsageCount;

            public MaterialValue(Material material, int usageCount)
            {
                Material = material;
                UsageCount = usageCount;
            }
        }

        /// <summary>
        /// Used as a key to material map.
        /// </summary>
        public class MaterialKey : IEquatable<MaterialKey>
        {
            /// <summary>
            /// Sprite's texture.
            /// </summary>
            public Texture2D Texture;

            /// <summary>
            /// Non instantiated material.
            /// </summary>
            public Material Material;

            public MaterialKey(Material material, Texture2D texture)
            {
                Material = material;
                Texture = texture;
            }

            private sealed class TextureMaterialEqualityComparer : IEqualityComparer<MaterialKey>
            {
                public bool Equals(MaterialKey x, MaterialKey y)
                {
                    if (ReferenceEquals(x, y)) return true;
                    if (ReferenceEquals(x, null)) return false;
                    if (ReferenceEquals(y, null)) return false;
                    if (x.GetType() != y.GetType()) return false;
                    return Equals(x.Texture, y.Texture) && Equals(x.Material, y.Material);
                }

                public int GetHashCode(MaterialKey obj)
                {
                    unchecked
                    {
                        return ((obj.Texture != null ? obj.Texture.GetHashCode() : 0)*397) ^ (obj.Material != null ? obj.Material.GetHashCode() : 0);
                    }
                }
            }

            private static readonly IEqualityComparer<MaterialKey> TextureMaterialComparerInstance = new TextureMaterialEqualityComparer();

            public static IEqualityComparer<MaterialKey> TextureMaterialComparer
            {
                get { return TextureMaterialComparerInstance; }
            }

            public bool Equals(MaterialKey other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(Texture, other.Texture) && Equals(Material, other.Material);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((MaterialKey) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Texture != null ? Texture.GetHashCode() : 0)*397) ^ (Material != null ? Material.GetHashCode() : 0);
                }
            }

            public static bool operator ==(MaterialKey left, MaterialKey right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(MaterialKey left, MaterialKey right)
            {
                return !Equals(left, right);
            }
        }
    }
}
