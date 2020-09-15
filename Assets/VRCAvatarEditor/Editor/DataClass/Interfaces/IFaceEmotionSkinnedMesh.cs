using System.Collections.Generic;
using UnityEngine;
using static VRCAvatarEditor.SkinnedMesh;

namespace VRCAvatarEditor
{
    public interface IFaceEmotionSkinnedMesh
    {
        SkinnedMeshRenderer Renderer { get; set; }

        Mesh Mesh { get; set; }
        GameObject Obj { get; set; }
        int BlendShapeCount { get; set; }
        bool IsOpenBlendShapes { get; set; }
        List<BlendShape> Blendshapes { get; set; }
        bool IsContainsAll { get; set; }
    }
}