using UnityEngine;

namespace VRCAvatarEditor
{
    public interface IProbeAnchorSkinnedMesh
    {
        SkinnedMeshRenderer Renderer { get; set; }

        GameObject Obj { get; set; }
    }
}