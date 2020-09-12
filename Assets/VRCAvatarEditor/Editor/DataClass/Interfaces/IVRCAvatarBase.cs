using System.Collections.Generic;
using UnityEngine;
using VRCAvatarEditor;
using LipSyncStyle = VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle;
using AnimationSet = VRC.SDKBase.VRC_AvatarDescriptor.AnimationSet;
using Object = UnityEngine.Object;
using System;

public interface IVRCAvatarBase
{
    Animator Animator { get; set; }
    Vector3 EyePos { get; set; }
    string AvatarId { get; set; }
    int OverridesNum { get; set; }
    SkinnedMeshRenderer FaceMesh { get; set; }
    List<string> LipSyncShapeKeyNames { get; set; }
    Material[] Materials { get; set; }
    int TriangleCount { get; set; }
    int TriangleCountInactive { get; set; }

    LipSyncStyle LipSyncStyle { get; set; }

    AnimationSet Sex { get; set; }

    Enum FaceShapeKeyEnum { get; set; }
    List<SkinnedMesh> SkinnedMeshList { get; set; }

    List<SkinnedMeshRenderer> SkinnedMeshRendererList { get; set; }
    List<MeshRenderer> MeshRendererList { get; set; }

    string AnimSavedFolderPath { get; set; }

    void LoadAvatarInfo(GameObject obj);
    void SetLipSyncToViseme();
    string GetAnimSavedFolderPath(Object asset);
}
