using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;
using VRCAvatarEditor.Base;

namespace VRCAvatarEditor.Test
{
    public class VRCAvatar2 : VRCAvatarBase
    {
        public VRC_AvatarDescriptor Descriptor { get; set; }

        public AnimatorOverrideController StandingAnimController { get; set; }
        public AnimatorOverrideController SittingAnimController { get; set; }

        public VRCAvatar2() {}
        public VRCAvatar2(VRC_AvatarDescriptor descriptor) {}
        public void LoadAvatarInfo() { }
        public void LoadAvatarInfo(VRC_AvatarDescriptor descriptor) { }

        public void SetAnimSavedFolderPath() { }

        public override void SetLipSyncToViseme() {}
    }
}