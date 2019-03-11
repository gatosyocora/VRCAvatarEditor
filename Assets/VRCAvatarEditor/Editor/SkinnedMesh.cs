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
        public Dictionary<int, string> blendshapes;
        public Dictionary<int, string> blendshapes_origin = null;

        public SkinnedMesh(SkinnedMeshRenderer m_renderer)
        {
            renderer = m_renderer;
            objName = renderer.gameObject.name;
            blendshapes = GetBlendShapes(renderer);
            blendShapeNum = blendshapes.Count;
            isOpenBlendShapes = (objName == "Body");

        }

        /// <summary>
        /// 特定のSkinnedMeshRendererが持つBlendShapeのリストを取得する
        /// </summary>
        /// <param name="skinnedMesh"></param>
        public Dictionary<int, string> GetBlendShapes(SkinnedMeshRenderer skinnedMesh)
        {
            Dictionary<int, string> blendshapes = new Dictionary<int, string>();
            var mesh = skinnedMesh.sharedMesh;
            blendShapeNum = mesh.blendShapeCount;
            if (blendShapeNum > 0)
            {
                for (int i = 0; i < blendShapeNum; i++)
                {
                    blendshapes.Add(i, mesh.GetBlendShapeName(i));
                }
            }

            return blendshapes;
        }

        /// <summary>
        /// blendshapesを昇順に並べ替える
        /// </summary>
        public void SortBlendShapes()
        {
            if (blendShapeNum <= 1) return;

            // 元のやつをコピーしておく
            if (blendshapes_origin == null)
                blendshapes_origin = new Dictionary<int, string>(blendshapes);

            blendshapes = blendshapes.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
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
