using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{

    public class SkinnedMesh
    {
        public SkinnedMeshRenderer renderer;
        public string objName;
        public int blendShapeNum;
        public bool isOpenBlendShapes;
        public List<BlendShape> blendshapes;
        public List<BlendShape> blendshapes_origin = null;
        public bool isContainsAll = true;

        public class BlendShape
        {
            public int id;
            public string name;
            public bool isContains;
            public bool isExclusion;

            public BlendShape(int id, string name, bool isContains)
            {
                this.id = id;
                this.name = name;
                this.isContains = isContains;
                this.isExclusion = false;
            }
        }

        public SkinnedMesh(SkinnedMeshRenderer m_renderer)
        {
            renderer = m_renderer;
            objName = renderer.gameObject.name;
            blendshapes = GetBlendShapes(renderer);
            blendShapeNum = blendshapes.Count;
            // 表情のメッシュのみtrueにする
            isOpenBlendShapes = (objName == "Body");
        }

        /// <summary>
        /// 特定のSkinnedMeshRendererが持つBlendShapeのリストを取得する
        /// </summary>
        /// <param name="skinnedMesh"></param>
        public List<BlendShape> GetBlendShapes(SkinnedMeshRenderer skinnedMesh)
        {
            List<BlendShape> blendshapes = new List<BlendShape>();
            var mesh = skinnedMesh.sharedMesh;
            blendShapeNum = mesh.blendShapeCount;
            if (blendShapeNum > 0)
            {
                for (int i = 0; i < blendShapeNum; i++)
                {
                    blendshapes.Add(new BlendShape(i, mesh.GetBlendShapeName(i), true));
                }
            }

            return blendshapes;
        }

        /// <summary>
        /// 名前にexclusionWordsが含まれるシェイプキーをリスト一覧表示から除外する設定にする
        /// </summary>
        /// <param name="exclusionWords"></param>
        public void SetExclusionBlendShapesByContains(List<string> exclusionWords)
        {
            for (int i = 0; i < blendShapeNum; i++)
            {
                blendshapes[i].isExclusion = false;

                // 除外するキーかどうか調べる
                foreach (var exclusionWord in exclusionWords)
                {
                    if (exclusionWord == "") continue;
                    if ((blendshapes[blendShapeIndex].name.ToLower()).Contains(exclusionWord.ToLower()))
                    {
                        blendshapes[i].isExclusion = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// blendshapesを昇順に並べ替える
        /// </summary>
        public void SortBlendShapes()
        {
            if (blendShapeNum <= 1) return;

            // 元のやつをコピーしておく
            if (blendshapes_origin == null)
                blendshapes_origin = new List<BlendShape>(blendshapes);

            blendshapes = blendshapes.OrderBy(x => x.name).ToList<BlendShape>();
        }

        /// <summary>
        /// blendshapesを初期状態の並び順に戻す
        /// </summary>
        public void ResetDefaultSort()
        {
            if (blendshapes_origin == null) return;

            blendshapes = blendshapes_origin;

            blendshapes_origin = null;
        }
    }

}
