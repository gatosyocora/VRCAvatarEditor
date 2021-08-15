using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
#if VRC_SDK_VRCSDK2
using VRCAvatar = VRCAvatarEditor.Avatars2.VRCAvatar2;
using AnimationsGUI = VRCAvatarEditor.Avatars2.AnimationsGUI2;
#else
using VRCAvatar = VRCAvatarEditor.Avatars3.VRCAvatar3;
using AnimationsGUI = VRCAvatarEditor.Avatars3.AnimationsGUI3;
#endif

namespace VRCAvatarEditor.Base
{
    public abstract class FaceEmotionGUIBase : Editor, IVRCAvatarEditorGUI
    {
        protected VRCAvatar editAvatar;
        protected VRCAvatar originalAvatar;
        protected VRCAvatarEditorGUI parentWindow;
        protected AnimationsGUI animationsGUI;

        protected static readonly string DEFAULT_ANIM_NAME = "faceAnim";
        protected HandPose.HandPoseType selectedHandAnim = HandPose.HandPoseType.NoSelection;

        protected Vector2 scrollPos = Vector2.zero;

        public enum SortType
        {
            UnSort,
            AToZ,
        }

        public SortType selectedSortType = SortType.UnSort;
        public List<string> blendshapeExclusions = new List<string> { "vrc.v_", "vrc.blink_", "vrc.lowerlid_", "vrc.owerlid_", "mmd" };

        protected bool isOpeningBlendShapeExclusionList = false;

        protected string animName;

        protected AnimationClip handPoseAnim;

        protected bool usePreviousAnimationOnHandAnimation;

        public void Initialize(VRCAvatar editAvatar, VRCAvatar originalAvatar, string saveFolderPath, EditorWindow window, AnimationsGUI animationsGUI)
        {
            this.editAvatar = editAvatar;
            this.originalAvatar = originalAvatar;
            animName = DEFAULT_ANIM_NAME;
            this.parentWindow = window as VRCAvatarEditorGUI;
            this.animationsGUI = animationsGUI;
        }

        public virtual bool DrawGUI(GUILayoutOption[] layoutOptions) 
        {
            EditorGUILayout.LabelField(LocalizeText.instance.langPair.faceEmotionTitle, EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                DrawFunctionButtons();

                if (editAvatar.SkinnedMeshList != null)
                {
                    BlendShapeListGUI();
                }

                DrawCreatedAnimationSettingsGUI();

                GUILayout.Space(20);

                DrawCreateButtonGUI();
            }

            return false;
        }

        protected void DrawFunctionButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                GatoGUILayout.Button(
                    LocalizeText.instance.langPair.loadAnimationButtonText,
                    () => {
                        FaceEmotion.LoadAnimationProperties(this, parentWindow);
                    },
                    editAvatar.Descriptor != null);

                GatoGUILayout.Button(
                    LocalizeText.instance.langPair.setToDefaultButtonText,
                    () => {
                        if (EditorUtility.DisplayDialog(
                                LocalizeText.instance.langPair.setToDefaultDialogTitleText,
                                LocalizeText.instance.langPair.setToDefaultDialogMessageText,
                                LocalizeText.instance.langPair.ok, LocalizeText.instance.langPair.cancel))
                        {
                            OnSetToDefaultButtonClick(editAvatar, originalAvatar);
                        }
                    },
                    editAvatar.Descriptor != null);

                GatoGUILayout.Button(
                    LocalizeText.instance.langPair.resetToDefaultButtonText,
                    () => {
                        FaceEmotion.ResetToDefaultFaceEmotion(editAvatar);
                        ChangeSaveAnimationState();
                    },
                    editAvatar.Descriptor != null);
            }
        }

        protected virtual void DrawCreatedAnimationSettingsGUI()
        {
            animName = EditorGUILayout.TextField(LocalizeText.instance.langPair.animClipFileNameLabel, animName);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(LocalizeText.instance.langPair.animClipSaveFolderLabel, originalAvatar.AnimSavedFolderPath);

                GatoGUILayout.Button(
                    LocalizeText.instance.langPair.selectFolder,
                    () => {
                        originalAvatar.AnimSavedFolderPath = EditorUtility.OpenFolderPanel(LocalizeText.instance.langPair.selectFolderDialogMessageText, originalAvatar.AnimSavedFolderPath, string.Empty);
                        originalAvatar.AnimSavedFolderPath = $"{FileUtil.GetProjectRelativePath(originalAvatar.AnimSavedFolderPath)}{Path.DirectorySeparatorChar}";
                        if (originalAvatar.AnimSavedFolderPath == $"{Path.DirectorySeparatorChar}") originalAvatar.AnimSavedFolderPath = $"Assets{Path.DirectorySeparatorChar}";
                        parentWindow.animationsGUI.UpdateSaveFolderPath(originalAvatar.AnimSavedFolderPath);
                    },
                    true,
                    GUILayout.Width(100));
            }

            EditorGUILayout.Space();
        }

        protected abstract void DrawCreateButtonGUI();

        public void DrawSettingsGUI()
        {
            EditorGUILayout.LabelField("FaceEmotion Creator", EditorStyles.boldLabel);

            selectedSortType = (SortType)EditorGUILayout.EnumPopup(LocalizeText.instance.langPair.sortTypeLabel, selectedSortType);

            isOpeningBlendShapeExclusionList = EditorGUILayout.Foldout(isOpeningBlendShapeExclusionList, LocalizeText.instance.langPair.blendShapeExclusionsLabel);
            if (isOpeningBlendShapeExclusionList)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    for (int i = 0; i < blendshapeExclusions.Count; i++)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            blendshapeExclusions[i] = EditorGUILayout.TextField(blendshapeExclusions[i]);
                            if (GUILayout.Button(LocalizeText.instance.langPair.remove))
                                blendshapeExclusions.RemoveAt(i);
                        }
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    GatoGUILayout.Button(
                        LocalizeText.instance.langPair.add,
                        () => {
                            blendshapeExclusions.Add(string.Empty);
                        });
                }
            }

            usePreviousAnimationOnHandAnimation = EditorGUILayout.ToggleLeft(LocalizeText.instance.langPair.usePreviousAnimationOnHandAnimationLabel, usePreviousAnimationOnHandAnimation);
        }

        public virtual void OnSetToDefaultButtonClick(VRCAvatar editAvatar, VRCAvatar originalAvatar)
        {
            FaceEmotion.SetToDefaultFaceEmotion(editAvatar, originalAvatar);
        }

        public abstract void ChangeSaveAnimationState();

        public void LoadSettingData(SettingData settingAsset)
        {
            selectedSortType = settingAsset.selectedSortType;
            blendshapeExclusions = new List<string>(settingAsset.blendshapeExclusions);
            usePreviousAnimationOnHandAnimation = settingAsset.usePreviousAnimationOnHandAnimation;
        }

        public void SaveSettingData(ref SettingData settingAsset)
        {
            settingAsset.selectedSortType = selectedSortType;
            settingAsset.blendshapeExclusions = new List<string>(blendshapeExclusions);
            settingAsset.usePreviousAnimationOnHandAnimation = usePreviousAnimationOnHandAnimation;
        }

        public void Dispose()
        {
            FaceEmotion.ResetToDefaultFaceEmotion(editAvatar);
        }

        protected void BlendShapeListGUI()
        {
            // BlendShapeのリスト
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos))
            {
                scrollPos = scrollView.scrollPosition;
                foreach (IFaceEmotionSkinnedMesh skinnedMesh in editAvatar.SkinnedMeshList)
                {
                    if (skinnedMesh.BlendShapeCount <= 0) continue;

                    skinnedMesh.IsOpenBlendShapes = EditorGUILayout.Foldout(skinnedMesh.IsOpenBlendShapes, skinnedMesh.Obj.name);
                    if (skinnedMesh.IsOpenBlendShapes)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                using (var check = new EditorGUI.ChangeCheckScope())
                                {
                                    skinnedMesh.IsContainsAll = EditorGUILayout.ToggleLeft(string.Empty, skinnedMesh.IsContainsAll, GUILayout.Width(45));
                                    if (check.changed)
                                    {
                                        FaceEmotion.SetContainsAll(skinnedMesh.IsContainsAll, skinnedMesh.Blendshapes);
                                    }
                                }
                                EditorGUILayout.LabelField(LocalizeText.instance.langPair.toggleAllLabel, GUILayout.Height(20));
                            }

                            foreach (var blendshape in skinnedMesh.Blendshapes)
                            {
                                if (!blendshape.IsExclusion)
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        blendshape.IsContains = EditorGUILayout.ToggleLeft(string.Empty, blendshape.IsContains, GUILayout.Width(45));

                                        EditorGUILayout.SelectableLabel(blendshape.Name, GUILayout.Height(20));
                                        using (var check = new EditorGUI.ChangeCheckScope())
                                        {
                                            var value = skinnedMesh.Renderer.GetBlendShapeWeight(blendshape.Id);
                                            value = EditorGUILayout.Slider(value, 0, 100);
                                            if (check.changed)
                                                skinnedMesh.Renderer.SetBlendShapeWeight(blendshape.Id, value);
                                        }

                                        GatoGUILayout.Button(
                                            LocalizeText.instance.langPair.minButtonText,
                                            () => {
                                                FaceEmotion.SetBlendShapeMinValue(skinnedMesh.Renderer, blendshape.Id);
                                            },
                                            true,
                                            GUILayout.MaxWidth(50));

                                        GatoGUILayout.Button(
                                            LocalizeText.instance.langPair.maxButtonText,
                                            () => {
                                                FaceEmotion.SetBlendShapeMaxValue(skinnedMesh.Renderer, blendshape.Id);
                                            },
                                            true,
                                            GUILayout.MaxWidth(50));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public virtual void OnLoadedAnimationProperties()
        {
            FaceEmotion.ApplyAnimationProperties(ScriptableSingleton<SendData>.instance.loadingProperties, editAvatar);
        }
    }
}