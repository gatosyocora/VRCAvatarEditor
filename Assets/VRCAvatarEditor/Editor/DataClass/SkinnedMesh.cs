using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{
    public class SkinnedMesh : IFaceEmotionSkinnedMesh, IProbeAnchorSkinnedMesh
    {
        public SkinnedMeshRenderer Renderer { get; set; }
        public Mesh Mesh { get; set; }
        public GameObject Obj { get; set; }
        public List<BlendShape> Blendshapes { get; set; }
        public int BlendShapeCount { get; set; }
        private List<BlendShape> UnSortedBlendshapes { get; set; } = null;　// null: blendshapesは未ソート
        public bool IsOpenBlendShapes { get; set; }
        public bool IsContainsAll { get; set; } = true;

        public class BlendShape
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool IsContains { get; set; }
            public bool IsExclusion { get; set; }

            public BlendShape(int id, string name, bool isContains)
            {
                this.Id = id;
                this.Name = name;
                this.IsContains = isContains;
                this.IsExclusion = false;
            }
        }

        public SkinnedMesh(SkinnedMeshRenderer renderer, GameObject faceMeshObj)
        {
            Renderer = renderer;
            Mesh = renderer.sharedMesh;
            Obj = Renderer.gameObject;
            Blendshapes = GetBlendShapes();
            BlendShapeCount = Blendshapes.Count;
            // 表情のメッシュのみtrueにする
            IsOpenBlendShapes = Obj.Equals(faceMeshObj);
        }

        /// <summary>
        /// 特定のSkinnedMeshRendererが持つBlendShapeのリストを取得する
        /// </summary>
        /// <param name="skinnedMesh"></param>
        private List<BlendShape> GetBlendShapes() =>
            Enumerable.Range(0, Mesh.blendShapeCount)
                .Select(i => new BlendShape(i, Mesh.GetBlendShapeName(i), true))
                .ToList();

        /// <summary>
        /// 名前にexclusionWordsが含まれるシェイプキーをリスト一覧表示から除外する設定にする
        /// </summary>
        /// <param name="exclusionWords"></param>
        public void SetExclusionBlendShapesByContains(IEnumerable<string> exclusionWords)
        {
            for (int blendShapeIndex = 0; blendShapeIndex < BlendShapeCount; blendShapeIndex++)
            {
                Blendshapes[blendShapeIndex].IsExclusion = false;

                // 除外するキーかどうか調べる
                foreach (var exclusionWord in exclusionWords)
                {
                    if (string.IsNullOrEmpty(exclusionWord)) continue;
                    if ((Blendshapes[blendShapeIndex].Name.ToLower()).Contains(exclusionWord.ToLower()))
                    {
                        Blendshapes[blendShapeIndex].IsExclusion = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// blendshapesを昇順に並べ替える
        /// </summary>
        public void SortBlendShapesToAscending()
        {
            if (BlendShapeCount <= 1) return;

            // 初期状態の並び順に戻すことがあるので初期状態のやつをコピーしておく
            if (UnSortedBlendshapes == null)
                UnSortedBlendshapes = new List<BlendShape>(Blendshapes);

            Blendshapes = Blendshapes.OrderBy(x => x.Name).ToList<BlendShape>();
        }

        /// <summary>
        /// blendshapesを初期状態の並び順に戻す
        /// </summary>
        public void ResetDefaultSort()
        {
            if (UnSortedBlendshapes == null) return;

            Blendshapes = UnSortedBlendshapes;

            UnSortedBlendshapes = null;
        }
    }

}
