using System;
using System.Collections.Generic;
using UnityEngine;

namespace Light2D.Examples
{
    public class BlockSetProfile : ScriptableObject
    {
        public enum BlockType
        {
            Empty,
            BackgroundWall,
            CollidingWall
        }

        public List<BlockInfo> BlockInfos = new List<BlockInfo>();

        //[Serializable]
        //public class BlockTilingInfo
        //{
        //    public bool T, L, B, R;
        //    public Sprite Sprite;

        //    public int Compact
        //    {
        //        get { return (T ? 1 : 0) + (R ? 2 : 0) + (B ? 4 : 0) + (L ? 8 : 0); }
        //    }
        //}

        public float FirstNoiseScale = 0.02f;
        public float SecondNoiseMul = 0.075f;
        public float SecondNoiseScale = 0.2f;

        [Serializable]
        public class BlockInfo
        {
            public GameObject AditionalObjectPrefab;
            public float AditionalObjectProbability;
            public BlockType BlockType;
            public Color LightAbsorption = new Color(0, 0, 0, 0);
            public Color LightEmission = new Color(0, 0, 0, 0);
            public float MaxNoise;
            public float MinNoise;
            public string Name;
            public Sprite[] SpriteInfo = new Sprite[0];
            public int Weight = 1;
        }

        //[ContextMenu("Fix sprite infos")]
        //private void FixSpriteInfos()
        //{
        //    foreach (var blockInfo in BlockInfos)
        //    {
        //        if (blockInfo.SpriteInfo.Length == 0)
        //        {
        //            blockInfo.SpriteInfo = new BlockTilingInfo[16];
        //            for (int i = 0; i < blockInfo.SpriteInfo.Length; i++)
        //            {
        //                var si = blockInfo.SpriteInfo[i] = new BlockTilingInfo();
        //                si.R = (i & 1) != 0;
        //                si.B = (i & 2) != 0;
        //                si.L = (i & 4) != 0;
        //                si.T = (i & 8) != 0;
        //            }
        //        }
        //    }
        //}
    }
}