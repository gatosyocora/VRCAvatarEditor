using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRCSDK2;
using System;

namespace VRCAvatarEditor
{
    public class Avatar
    {
        public Animator animator { get; set; }
        public VRC_AvatarDescriptor descriptor { get; set; }
        public Vector3 eyePos { get; set; }
        public AnimatorOverrideController standingAnimController { get; set; }
        public AnimatorOverrideController sittingAnimController { get; set; }
        public VRC_AvatarDescriptor.AnimationSet sex { get; set; }
        public string avatarId { get; set; }
        public int overridesNum { get; set; }
        public SkinnedMeshRenderer faceMesh { get; set; }
        public List<string> lipSyncShapeKeyNames;
        public List<Material> materials { get; set; }
        public int triangleCount { get; set; }
        public int triangleCountInactive { get; set; }
        public VRC_AvatarDescriptor.LipSyncStyle lipSyncStyle { get; set; }
        public Enum faceShapeKeyEnum { get; set; }

        public Avatar()
        {
            animator = null;
            descriptor = null;
            eyePos = Vector3.zero;
            standingAnimController = null;
            sittingAnimController = null;
            sex = VRC_AvatarDescriptor.AnimationSet.None;
            avatarId = string.Empty;
            overridesNum = 0;
            faceMesh = null;
            lipSyncShapeKeyNames = null;
            triangleCount = 0;
            triangleCountInactive = 0;
            lipSyncStyle = VRC_AvatarDescriptor.LipSyncStyle.Default;
            faceShapeKeyEnum = null;
        }

    }
}

