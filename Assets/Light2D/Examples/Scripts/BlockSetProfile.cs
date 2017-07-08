using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Light2D.Examples
{
    public class BlockSetProfile : ScriptableObject
    {
        public enum BlockType
        {
            Empty,
            BackgroundWall,
            CollidingWall,
        }

        [Serializable]
        public class BlockInfo
        {
            public string Name;
            public float MinNoise;
            public float MaxNoise;
            public BlockType BlockType;
            public int Weight = 1;
            public Color LightEmission = new Color(0, 0, 0, 0);
            public Color LightAbsorption = new Color(0, 0, 0, 0);
            public float AditionalObjectProbability;
            public GameObject AditionalObjectPrefab;
            public Sprite[] SpriteInfo = new Sprite[0];
        }

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
        public float SecondNoiseScale = 0.2f;
        public float SecondNoiseMul = 0.075f;
        public List<BlockInfo> BlockInfos = new List<BlockInfo>();

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