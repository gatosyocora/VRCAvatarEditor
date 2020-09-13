using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRCAvatarEditor.Base
{
    public abstract class AnimationsGUIBase : Editor, IVRCAvatarEditorGUI
    {
        protected VRCAvatarEditorGUI vrcAvatarEditorGUI;
        protected FaceEmotionGUI faceEmotionGUI;

        protected string saveFolderPath;

        public abstract bool DrawGUI(GUILayoutOption[] layoutOptions);

        public virtual void DrawSettingsGUI() { }
        public virtual void LoadSettingData(SettingData settingAsset) { }
        public virtual void SaveSettingData(ref SettingData settingAsset) { }
        public virtual void Dispose() { }

        public void UpdateSaveFolderPath(string saveFolderPath)
        {
            this.saveFolderPath = saveFolderPath;
        }
    }
}