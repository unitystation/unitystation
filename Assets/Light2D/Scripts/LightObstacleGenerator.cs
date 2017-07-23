using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Light2D
{
    /// <summary>
    /// That class is generating obstacles for object it attached to.
    /// Obect must have MeshRenderer, SpriteRenderer or CustomSprite script from which texture for obstacle will be grabbed.
    /// For rendering obstacle of SpriteRenderer and CustomSprite LightObstacleSprite with material "Material" (material with dual color shader by default) will be used.
    /// For objects with MeshRenderer "Material" property is ignored. MeshRenderer.sharedMaterial is used instead.
    /// </summary>
    [ExecuteInEditMode]
    public class LightObstacleGenerator : MonoBehaviour
    {
        /// <summary>
        /// Vertex color.
        /// </summary>
        public Color MultiplicativeColor = new Color(0, 0, 0, 1);

        /// <summary>
        /// AdditiveColor that will be used for obstacle when SpriteRenderer or CustomSprite scripts is attached.
        /// Only DualColor shader supports additive color.
        /// </summary>
        public Color AdditiveColor;

        /// <summary>
        /// Material that will be used for obstacle when SpriteRenderer or CustomSprite scripts is attached.
        /// </summary>
        public Material Material;

        public float LightObstacleScale = 1;

        private void Start()
        {
#if UNITY_EDITOR
            if (Material == null)
            {
                Material = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Light2D/Materials/DualColor.mat", typeof(Material));
            }
#endif

            if (!Application.isPlaying)
                return;

            var obstacleObj = new GameObject(gameObject.name + " Light Obstacle");

            obstacleObj.transform.parent = gameObject.transform;
            obstacleObj.transform.localPosition = Vector3.zero;
            obstacleObj.transform.localRotation = Quaternion.identity;
            obstacleObj.transform.localScale = Vector3.one*LightObstacleScale;
            if (LightingSystem.Instance != null)
                obstacleObj.layer = LightingSystem.Instance.LightObstaclesLayer;

            if (GetComponent<SpriteRenderer>() != null || GetComponent<CustomSprite>() != null)
            {
                var obstacleSprite = obstacleObj.AddComponent<LightObstacleSprite>();
                obstacleSprite.Color = MultiplicativeColor;
                obstacleSprite.AdditiveColor = AdditiveColor;
                obstacleSprite.Material = Material;
            }
            else
            {
                var obstacleMesh = obstacleObj.AddComponent<LightObstacleMesh>();
                obstacleMesh.MultiplicativeColor = MultiplicativeColor;
                obstacleMesh.AdditiveColor = AdditiveColor;
                obstacleMesh.Material = Material;
            }

            Destroy(this);
        }
    }
}
