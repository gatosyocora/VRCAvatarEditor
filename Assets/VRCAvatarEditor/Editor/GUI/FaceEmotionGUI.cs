using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using VRCAvatarEditor;
using System;

namespace VRCAvatarEditor
{
    public class FaceEmotionGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatarEditor.Avatar editAvatar;
        private VRCAvatarEditor.Avatar originalAvatar;
        private VRCAvatarEditorGUI parentWindow;

        private static readonly string DEFAULT_ANIM_NAME = "faceAnim";
        private HandPose.HandPoseType selectedHandAnim = HandPose.HandPoseType.None;

        private Vector2 scrollPos = Vector2.zero;

        public enum SortType
        {
            UnSort,
            AToZ,
        }

        public SortType selectedSortType = SortType.UnSort;
        public List<string> blendshapeExclusions = new List<string> { "vrc.v_", "vrc.blink_", "vrc.lowerlid_", "vrc.owerlid_", "mmd" };

        private bool isOpeningBlendShapeExclusionList = false;

        private string animName;

        private AnimationClip handPoseAnim;

        public void Initialize(ref VRCAvatarEditor.Avatar editAvatar, VRCAvatarEditor.Avatar originalAvatar, string saveFolderPath, EditorWindow window)
        {
            this.editAvatar = editAvatar;
            this.originalAvatar = originalAvatar;
            animName = DEFAULT_ANIM_NAME;
            this.parentWindow = window as VRCAvatarEditorGUI;
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            EditorGUILayout.LabelField("表情設定", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUI.DisabledScope(editAvatar.descriptor == null))
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Load Animation"))
                    {
                        FaceEmotion.LoadAnimationProperties(this, parentWindow);
                    }

                    if (GUILayout.Button("Set To Default"))
                    {
                        if (EditorUtility.DisplayDialog(
                                "Default FaceEmotion Setting", 
                                "現在の表情をデフォルトに設定しますか", 
                                "OK", "Cancel"))
                        {
                            FaceEmotion.SetToDefaultFaceEmotion(ref editAvatar, originalAvatar);
                        }
                    }

                    if (GUILayout.Button("Reset To Default"))
                    {
                        FaceEmotion.ResetToDefaultFaceEmotion(ref editAvatar);
                    }
                }

                if (editAvatar.skinnedMeshList != null)
                {
                    BlendShapeListGUI();
                }

                animName = EditorGUILayout.TextField("AnimClipFileName", animName);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("AnimClipSaveFolder", originalAvatar.animSavedFolderPath);

                    if (GUILayout.Button("Select Folder", GUILayout.Width(100)))
                    {
                        originalAvatar.animSavedFolderPath = EditorUtility.OpenFolderPanel("Select saved folder", originalAvatar.animSavedFolderPath, string.Empty);
                        originalAvatar.animSavedFolderPath = FileUtil.GetProjectRelativePath(originalAvatar.animSavedFolderPath) + "/";
                        if (originalAvatar.animSavedFolderPath == "/") originalAvatar.animSavedFolderPath = "Assets/";
                        parentWindow.animationsGUI.UpdateSaveFolderPath(originalAvatar.animSavedFolderPath);
                    }

                }

                EditorGUILayout.Space();

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    selectedHandAnim = (HandPose.HandPoseType)Enum.ToObject(typeof(HandPose.HandPoseType), EditorGUILayout.Popup(
                        "Set Index(仮名)",
                        (int)selectedHandAnim, 
                        Enum.GetNames(typeof(HandPose.HandPoseType)).Select((x, index) => index + ":"+x).ToArray()));

                    if (check.changed)
                    {
                        handPoseAnim = HandPose.GetHandAnimationClip(selectedHandAnim);
                    }
                }

                handPoseAnim = EditorGUILayout.ObjectField("HandPose AnimClip", handPoseAnim, typeof(AnimationClip), true) as AnimationClip;

                EditorGUILayout.Space();

                using (new EditorGUI.DisabledGroupScope(
                            selectedHandAnim == HandPose.HandPoseType.None ||
                            handPoseAnim == null))
                {
                    if (GUILayout.Button("Create AnimFile"))
                    {
                        var animController = originalAvatar.standingAnimController;

                        var createdAnimClip = FaceEmotion.CreateBlendShapeAnimationClip(animName, originalAvatar.animSavedFolderPath, ref editAvatar, ref blendshapeExclusions, editAvatar.descriptor.gameObject);
                        if (selectedHandAnim != HandPose.HandPoseType.None)
                        {
                            //HandPose.AddHandPoseAnimationKeysFromOriginClip(ref createdAnimClip, selectedHandAnim);
                            FaceEmotion.CopyAnimationKeysFromOriginClip(createdAnimClip, handPoseAnim);
                            animController[AnimationsGUI.HANDANIMS[(int)selectedHandAnim - 1]] = createdAnimClip;

                            FaceEmotion.ResetToDefaultFaceEmotion(ref editAvatar);
                        }

                        originalAvatar.standingAnimController = animController;
                        editAvatar.standingAnimController = animController;
                    }
                }

            }

            return false;
        }

        public void DrawSettingsGUI()
        {
            EditorGUILayout.LabelField("FaceEmotion Creator", EditorStyles.boldLabel);

            selectedSortType = (SortType)EditorGUILayout.EnumPopup("SortType", selectedSortType);

            isOpeningBlendShapeExclusionList = EditorGUILayout.Foldout(isOpeningBlendShapeExclusionList, "Blendshape Exclusions");
            if (isOpeningBlendShapeExclusionList)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    for (int i = 0; i < blendshapeExclusions.Count; i++)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            blendshapeExclusions[i] = EditorGUILayout.TextField(blendshapeExclusions[i]);
                            if (GUILayout.Button("Remove"))
                                blendshapeExclusions.RemoveAt(i);
                        }
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add"))
                        blendshapeExclusions.Add(string.Empty);
                }
            }
        }

        public void LoadSettingData(SettingData settingAsset)
        {
            selectedSortType = settingAsset.selectedSortType;
            blendshapeExclusions = new List<string>(settingAsset.blendshapeExclusions);
        }

        public void SaveSettingData(ref SettingData settingAsset)
        {
            settingAsset.selectedSortType = selectedSortType;
            settingAsset.blendshapeExclusions = new List<string>(blendshapeExclusions);
        }

        public void Dispose() 
        {
            FaceEmotion.ResetToDefaultFaceEmotion(ref editAvatar);
        }

        private void BlendShapeListGUI()
        {
            // BlendShapeのリスト
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos))
            {
                scrollPos = scrollView.scrollPosition;
                foreach (var skinnedMesh in editAvatar.skinnedMeshList)
                {
                    skinnedMesh.isOpenBlendShapes = EditorGUILayout.Foldout(skinnedMesh.isOpenBlendShapes, skinnedMesh.obj.name);
                    if (skinnedMesh.isOpenBlendShapes)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                using (var check = new EditorGUI.ChangeCheckScope())
                                {
                                    skinnedMesh.isContainsAll = EditorGUILayout.ToggleLeft(string.Empty, skinnedMesh.isContainsAll, GUILayout.Width(45));
                                    if (check.changed)
                                    {
                                        FaceEmotion.SetContainsAll(skinnedMesh.isContainsAll, ref skinnedMesh.blendshapes);
                                    }
                                }
                                EditorGUILayout.LabelField("Toggle All", GUILayout.Height(20));
                            }

                            foreach (var blendshape in skinnedMesh.blendshapes)
                            {
                                if (!blendshape.isExclusion)
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        blendshape.isContains = EditorGUILayout.ToggleLeft(string.Empty, blendshape.isContains, GUILayout.Width(45));

                                        EditorGUILayout.SelectableLabel(blendshape.name, GUILayout.Height(20));
                                        using (var check = new EditorGUI.ChangeCheckScope())
                                        {
                                            var value = skinnedMesh.renderer.GetBlendShapeWeight(blendshape.id);
                                            value = EditorGUILayout.Slider(value, 0, 100);
                                            if (check.changed)
                                                skinnedMesh.renderer.SetBlendShapeWeight(blendshape.id, value);
                                        }

                                        if (GUILayout.Button("Min", GUILayout.MaxWidth(50)))
                                        {
                                            FaceEmotion.SetBlendShapeMinValue(ref skinnedMesh.renderer, blendshape.id);
                                        }
                                        if (GUILayout.Button("Max", GUILayout.MaxWidth(50)))
                                        {
                                            FaceEmotion.SetBlendShapeMaxValue(ref skinnedMesh.renderer, blendshape.id);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OnLoadedAnimationProperties()
        {
            FaceEmotion.ApplyAnimationProperties(ScriptableSingleton<SendData>.instance.loadingProperties, ref editAvatar);
        }
    }
}
