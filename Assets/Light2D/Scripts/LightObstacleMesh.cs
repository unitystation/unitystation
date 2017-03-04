using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Light2D
{
    /// <summary>
    /// Automatically updating mesh, material and main texture of light obstacle. 
    /// Class is copying all data used for rendering from parent.
    /// </summary>
    public class LightObstacleMesh : MonoBehaviour
    {
        public Color32 MultiplicativeColor;
        public Color AdditiveColor;
        public Material Material;
        private MeshRenderer _parentMeshRenderer;
        private MeshFilter _parentMeshFilter;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private Mesh _oldParentMesh;
        private Color32 _oldMulColor;
        private Color _oldAddColor;
        private Material _oldMaterial;
        private CustomSprite.MaterialKey _materialKey;

        void Awake()
        {
            _parentMeshRenderer = transform.parent.GetComponent<MeshRenderer>();
            _parentMeshFilter = transform.parent.GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null) _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null) _meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        void Update()
        {
            Refresh();
        }

        void Refresh()
        {
            if (_parentMeshFilter == null || _parentMeshFilter == null || _meshRenderer == null || _meshFilter == null ||
                _parentMeshFilter.sharedMesh == null || _parentMeshRenderer.sharedMaterial == null)
            {
                if (_meshRenderer != null)
                    _meshRenderer.enabled = false;
                return;
            }

            bool dirty = false;
            if (_parentMeshFilter.mesh != _oldParentMesh)
            {
                if (_meshFilter.mesh != null)
                    Destroy(_meshFilter.mesh);
                _meshFilter.mesh = (Mesh) Instantiate(_parentMeshFilter.sharedMesh);
                _meshFilter.mesh.MarkDynamic();

                if (_meshFilter.mesh.tangents == null)
                {
                    var tangents = new Vector4[_meshFilter.mesh.vertexCount];
                    for (int i = 0; i < tangents.Length; i++)
                        tangents[i] = new Vector4(1, 0);
                    _meshFilter.mesh.tangents = tangents;
                }

                _oldParentMesh = _parentMeshFilter.sharedMesh;
                dirty = true;
            }

            if (_oldMaterial != _parentMeshRenderer.sharedMaterial ||
                (_oldMaterial != null && _parentMeshRenderer.sharedMaterial != null &&
                 _oldMaterial.mainTexture != _parentMeshRenderer.sharedMaterial.mainTexture))
            {
                if (_meshRenderer.sharedMaterial != null && _materialKey != null)
                {
                    CustomSprite.ReleaseMaterial(_materialKey);
                }
                var baseMat = Material == null ? _parentMeshRenderer.sharedMaterial : Material;
                var tex = _parentMeshRenderer.sharedMaterial.mainTexture as Texture2D;
                _meshRenderer.sharedMaterial = CustomSprite.GetOrCreateMaterial(baseMat, tex, out _materialKey);
                _oldMaterial = _parentMeshRenderer.sharedMaterial;
            }

            if (!MultiplicativeColor.Equals(_oldMulColor) || AdditiveColor != _oldAddColor || dirty)
            {
                var colors = _meshFilter.mesh.colors32;
                if (colors == null || colors.Length != _meshFilter.mesh.vertexCount)
                    colors = new Color32[_meshFilter.mesh.vertexCount];

                for (int i = 0; i < colors.Length; i++)
                    colors[i] = MultiplicativeColor;
                _meshFilter.mesh.colors32 = colors;

                var uv1 = new Vector2(
                    Util.DecodeFloatRGBA((Vector4) AdditiveColor),
                    Util.DecodeFloatRGBA(new Vector4(AdditiveColor.a, 0, 0)));
                var uv1Arr = _meshFilter.mesh.uv2;
                if (uv1Arr == null || uv1Arr.Length != colors.Length)
                    uv1Arr = new Vector2[colors.Length];
                for (int i = 0; i < uv1Arr.Length; i++)
                {
                    uv1Arr[i] = uv1;
                }
                _meshFilter.mesh.uv2 = uv1Arr;

                _oldMulColor = MultiplicativeColor;
                _oldAddColor = AdditiveColor;
            }
        }
    }
}
