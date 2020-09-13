#if VRC_SDK_VRCSDK2
using VRCAvatarEditor.Base;
using System;
using System.Collections.Generic;
using UnityEngine;
using VRCSDK2;
using LipSyncStyle = VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle;
using AnimationSet = VRC.SDKBase.VRC_AvatarDescriptor.AnimationSet;
using Viseme = VRC.SDKBase.VRC_AvatarDescriptor.Viseme;

namespace VRCAvatarEditor.Avatars2
{
    public class VRCAvatar2 : VRCAvatarBase
    {
        public VRC_AvatarDescriptor Descriptor { get; set; }

        public AnimatorOverrideController StandingAnimController { get; set; }
        public AnimatorOverrideController SittingAnimController { get; set; }

        public VRCAvatar2() : base()
        {
            Descriptor = null;

            StandingAnimController = null;
            SittingAnimController = null;
        }

        public VRCAvatar2(VRC_AvatarDescriptor descriptor) : this()
        {
            if (descriptor == null) return;
            LoadAvatarInfo(descriptor);
        }

        public void LoadAvatarInfo()
        {
            LoadAvatarInfo(Descriptor);
        }

        public void LoadAvatarInfo(VRC_AvatarDescriptor descriptor)
        {
            if (descriptor == null) return;
            Descriptor = descriptor;

            StandingAnimController = Descriptor.CustomStandingAnims;
            SittingAnimController = Descriptor.CustomSittingAnims;

            AnimSavedFolderPath = GetAnimSavedFolderPath(StandingAnimController);
            FaceMesh = Descriptor.VisemeSkinnedMesh;

            if (FaceMesh != null && Descriptor.lipSync == LipSyncStyle.VisemeBlendShape)
            {
                LipSyncShapeKeyNames = new List<string>();
                LipSyncShapeKeyNames.AddRange(Descriptor.VisemeBlendShapes);
            }

            LipSyncStyle = Descriptor.lipSync;

            EyePos = Descriptor.ViewPosition;
            Sex = Descriptor.Animations;

            base.LoadAvatarInfo(Descriptor.gameObject);
        }

        public override void SetLipSyncToViseme()
        {
            if (Descriptor == null) return;

            LipSyncStyle = LipSyncStyle.VisemeBlendShape;
            Descriptor.lipSync = LipSyncStyle.VisemeBlendShape;

            if (FaceMesh == null)
            {
                var rootObj = Animator.gameObject;
                FaceMesh = rootObj.GetComponentInChildren<SkinnedMeshRenderer>();
                Descriptor.VisemeSkinnedMesh = FaceMesh;
            }

            if (FaceMesh == null) return;
            var mesh = FaceMesh.sharedMesh;

            var visemeBlendShapeNames = Enum.GetNames(typeof(Viseme));

            for (int visemeIndex = 0; visemeIndex < visemeBlendShapeNames.Length; visemeIndex++)
            {
                // VRC用アバターとしてよくあるシェイプキーの名前を元に自動設定
                var visemeShapeKeyName = "vrc.v_" + visemeBlendShapeNames[visemeIndex];
                if (mesh.GetBlendShapeIndex(visemeShapeKeyName) != -1)
                {
                    Descriptor.VisemeBlendShapes[visemeIndex] = visemeShapeKeyName;
                    continue;
                }

                visemeShapeKeyName = "VRC.v_" + visemeBlendShapeNames[visemeIndex];
                if (mesh.GetBlendShapeIndex(visemeShapeKeyName) != -1)
                {
                    Descriptor.VisemeBlendShapes[visemeIndex] = visemeShapeKeyName;
                }
            }
        }

        public void SetAnimSavedFolderPath()
        {
            AnimSavedFolderPath = GetAnimSavedFolderPath(StandingAnimController);
        }

    }
}
#endif