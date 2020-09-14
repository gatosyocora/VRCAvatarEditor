using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRCAvatarEditor.Base;
#if VRC_SDK_VRCSDK2
using VRCAvatar = VRCAvatarEditor.Avatars2.VRCAvatar2;
using AnimationsGUI = VRCAvatarEditor.Avatars2.AnimationsGUI2;
#else
using VRCAvatar = VRCAvatarEditor.Avatars3.VRCAvatar3;
using AnimationsGUI = VRCAvatarEditor.Avatars3.AnimationsGUI3;
#endif

namespace VRCAvatarEditor.Avatars2
{
    public class FaceEmotionGUI2 : FaceEmotionGUIBase
    {
        public override bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            EditorGUILayout.LabelField(LocalizeText.instance.langPair.faceEmotionTitle, EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
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
                                FaceEmotion.SetToDefaultFaceEmotion(editAvatar, originalAvatar);
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

                if (editAvatar.SkinnedMeshList != null)
                {
                    BlendShapeListGUI();
                }

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

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    selectedHandAnim = (HandPose.HandPoseType)Enum.ToObject(typeof(HandPose.HandPoseType), EditorGUILayout.Popup(
                        LocalizeText.instance.langPair.animationOverrideLabel,
                        (int)selectedHandAnim,
                        Enum.GetNames(typeof(HandPose.HandPoseType)).Select((x, index) => index + ":" + x).ToArray()));

                    if (check.changed)
                    {
                        ChangeSelectionHandAnimation();
                    }
                }

                using (new EditorGUI.DisabledGroupScope(selectedHandAnim == HandPose.HandPoseType.NoSelection))
                {
                    handPoseAnim = GatoGUILayout.ObjectField(
                                        LocalizeText.instance.langPair.handPoseAnimClipLabel,
                                        handPoseAnim);
                }

                GUILayout.Space(20);

                GatoGUILayout.Button(
                    LocalizeText.instance.langPair.createAnimFileButtonText,
                    () => {
#if VRC_SDK_VRCSDK2
                        var animController = originalAvatar.StandingAnimController;

                        var createdAnimClip = FaceEmotion.CreateBlendShapeAnimationClip(animName, originalAvatar.AnimSavedFolderPath, editAvatar, blendshapeExclusions, editAvatar.Descriptor.gameObject);
                        if (selectedHandAnim != HandPose.HandPoseType.NoSelection)
                        {
                            HandPose.AddHandPoseAnimationKeysFromOriginClip(createdAnimClip, handPoseAnim);
                            animController[AnimationsGUI.HANDANIMS[(int)selectedHandAnim - 1]] = createdAnimClip;
                            EditorUtility.SetDirty(animController);

                            FaceEmotion.ResetToDefaultFaceEmotion(editAvatar);
                        }

                        originalAvatar.StandingAnimController = animController;
                        editAvatar.StandingAnimController = animController;

                        animationsGUI.ResetPathMissing(AnimationsGUI.HANDANIMS[(int)selectedHandAnim - 1]);
#endif
                    },
                    selectedHandAnim != HandPose.HandPoseType.NoSelection &&
                    handPoseAnim != null);
            }

            return false;
        }

        public override void OnLoadedAnimationProperties()
        {
            base.OnLoadedAnimationProperties();
            ChangeSaveAnimationState();
        }

        public void ChangeSaveAnimationState(
                string animName = "",
#if VRC_SDK_VRCSDK2
                HandPose.HandPoseType selectedHandAnim = HandPose.HandPoseType.NoSelection,
#elif VRC_SDK_VRCSDK3
                int selectStateIndex = 0,
#endif
                AnimationClip handPoseAnim = null)
        {
            this.animName = animName;
#if VRC_SDK_VRCSDK2
            this.selectedHandAnim = selectedHandAnim;
#elif VRC_SDK_VRCSDK3
            // TODO: 一時対処
            this.selectedHandAnim = HandPose.HandPoseType.NoSelection;
#endif
            if (handPoseAnim is null)
                handPoseAnim = HandPose.GetHandAnimationClip(this.selectedHandAnim);
            this.handPoseAnim = handPoseAnim;
        }

        private void ChangeSelectionHandAnimation()
        {
#if VRC_SDK_VRCSDK2
            if (usePreviousAnimationOnHandAnimation)
            {
                var animController = originalAvatar.StandingAnimController;
                var previousAnimation = animController[AnimationsGUI.HANDANIMS[(int)selectedHandAnim - 1]];

                // 未設定でなければ以前設定されていたものをHandPoseAnimationとして使う
                if (previousAnimation != null && previousAnimation.name != AnimationsGUI.HANDANIMS[(int)selectedHandAnim - 1])
                {
                    handPoseAnim = previousAnimation;
                }
                else
                {
                    handPoseAnim = HandPose.GetHandAnimationClip(selectedHandAnim);
                }
            }
            else
            {
                handPoseAnim = HandPose.GetHandAnimationClip(selectedHandAnim);
            }
#endif
        }
    }
}
