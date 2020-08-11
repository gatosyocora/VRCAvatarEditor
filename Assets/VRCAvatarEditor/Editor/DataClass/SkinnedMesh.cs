using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{

    public class SkinnedMesh
    {
        public SkinnedMeshRenderer Renderer { get; set; }
        public Mesh Mesh { get; set; }
        public GameObject Obj { get; set; }
        public int BlendShapeNum { get; set; }
        public bool IsOpenBlendShapes { get; set; }
        public List<BlendShape> Blendshapes { get; set; }
        public List<BlendShape> BlendshapesOrigin { get; set; } = null;　// null: blendshapesは未ソート
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

        public SkinnedMesh(SkinnedMeshRenderer m_renderer, GameObject faceMeshObj)
        {
            Renderer = m_renderer;
            Mesh = m_renderer.sharedMesh;
            Obj = Renderer.gameObject;
            Blendshapes = GetBlendShapes();
            BlendShapeNum = Blendshapes.Count;
            // 表情のメッシュのみtrueにする
            IsOpenBlendShapes = Obj.Equals(faceMeshObj);
        }

        /// <summary>
        /// 特定のSkinnedMeshRendererが持つBlendShapeのリストを取得する
        /// </summary>
        /// <param name="skinnedMesh"></param>
        public List<BlendShape> GetBlendShapes()
        {
            List<BlendShape> blendshapes = new List<BlendShape>();

            for (int blendShapeIndex = 0; blendShapeIndex < Mesh.blendShapeCount; blendShapeIndex++)
            {
                blendshapes.Add(new BlendShape(blendShapeIndex, Mesh.GetBlendShapeName(blendShapeIndex), true));
            }

            return blendshapes;
        }

        /// <summary>
        /// 名前にexclusionWordsが含まれるシェイプキーをリスト一覧表示から除外する設定にする
        /// </summary>
        /// <param name="exclusionWords"></param>
        public void SetExclusionBlendShapesByContains(List<string> exclusionWords)
        {
            for (int blendShapeIndex = 0; blendShapeIndex < BlendShapeNum; blendShapeIndex++)
            {
                Blendshapes[blendShapeIndex].IsExclusion = false;

                // 除外するキーかどうか調べる
                foreach (var exclusionWord in exclusionWords)
                {
                    if (exclusionWord == "") continue;
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
            if (BlendShapeNum <= 1) return;

            // 初期状態の並び順に戻すことがあるので初期状態のやつをコピーしておく
            if (BlendshapesOrigin == null)
                BlendshapesOrigin = new List<BlendShape>(Blendshapes);

            Blendshapes = Blendshapes.OrderBy(x => x.Name).ToList<BlendShape>();
        }

        /// <summary>
        /// blendshapesを初期状態の並び順に戻す
        /// </summary>
        public void ResetDefaultSort()
        {
            if (BlendshapesOrigin == null) return;

            Blendshapes = BlendshapesOrigin;

            BlendshapesOrigin = null;
        }
    }

}
