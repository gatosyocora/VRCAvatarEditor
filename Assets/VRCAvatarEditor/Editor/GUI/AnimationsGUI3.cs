#if VRC_SDK_VRCSDK3
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRCAvatarEditor.Base;
using VRCAvatarEditor.Utilitys;
using VRCAvatar = VRCAvatarEditor.Avatars3.VRCAvatar3;

namespace VRCAvatarEditor.Avatars3
{
    public class AnimationsGUI3 : AnimationsGUIBase
    {
        private VRCAvatar editAvatar;
        private VRCAvatar originalAvatar;

        public void Initialize(VRCAvatar editAvatar,
                               VRCAvatar originalAvatar,
                               string saveFolderPath,
                               VRCAvatarEditorGUI vrcAvatarEditorGUI,
                               FaceEmotionGUI faceEmotionGUI)
        {
            this.editAvatar = editAvatar;
            this.originalAvatar = originalAvatar;
            this.vrcAvatarEditorGUI = vrcAvatarEditorGUI;
            this.faceEmotionGUI = faceEmotionGUI;
            UpdateSaveFolderPath(saveFolderPath);
        }

        public override bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            return false;
        }
    }
}
#endif