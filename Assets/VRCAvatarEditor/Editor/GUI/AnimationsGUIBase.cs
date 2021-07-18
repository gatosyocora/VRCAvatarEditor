using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRCAvatarEditor.Base
{
    public abstract class AnimationsGUIBase : Editor, IVRCAvatarEditorGUI
    {
        protected VRCAvatarEditorGUI vrcAvatarEditorGUI;
        protected FaceEmotionGUIBase faceEmotionGUI;

        protected string saveFolderPath;

        private GUIStyle normalStyle = new GUIStyle();
        private GUIStyle errorStyle = new GUIStyle();

        protected void Initialize(string saveFolderPath)
        {
            errorStyle.normal.textColor = Color.red;

            UpdateSaveFolderPath(saveFolderPath);
        }

        public abstract bool DrawGUI(GUILayoutOption[] layoutOptions);

        public virtual void DrawSettingsGUI() { }
        public virtual void LoadSettingData(SettingData settingAsset) { }
        public virtual void SaveSettingData(ref SettingData settingAsset) { }
        public virtual void Dispose() { }

        protected AnimationClip ValidatableAnimationField(string label, AnimationClip clip, bool hasError, Action contents = null)
        {
            AnimationClip retClip;
            using (new EditorGUILayout.HorizontalScope(GUILayout.Width(350)))
            {
                EditorGUILayout.LabelField(label, hasError ? errorStyle : normalStyle, GUILayout.Width(100));

                retClip = GatoGUILayout.ObjectField(
                    string.Empty,
                    clip,
                    true,
                    GUILayout.Width(200));

                contents?.Invoke();
            }
            return retClip;
        }

        protected AnimationClip EdittableAnimationField(string label, AnimationClip clip, bool hasError, bool edittable, Action OnEdit)
        {
            return ValidatableAnimationField(label, clip, hasError, () =>
            {
                GatoGUILayout.Button(
                    LocalizeText.instance.langPair.edit,
                    () => OnEdit(),
                    edittable,
                    GUILayout.Width(50));
            });
        }

        public void UpdateSaveFolderPath(string saveFolderPath)
        {
            this.saveFolderPath = saveFolderPath;
        }
    }
}