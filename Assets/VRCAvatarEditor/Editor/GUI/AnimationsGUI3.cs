﻿#if VRC_SDK_VRCSDK3
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRCAvatarEditor.Base;
using VRCAvatarEditor.Utilities;
using VRCAvatar = VRCAvatarEditor.Avatars3.VRCAvatar3;
using VRC.SDK3.Avatars.Components;

namespace VRCAvatarEditor.Avatars3
{
    public class AnimationsGUI3 : AnimationsGUIBase
    {
        private VRCAvatar editAvatar;
        private VRCAvatar originalAvatar;

        public static readonly string[] EMOTIONSTATES = { "Idle", "Fist", "Open", "Point", "Peace", "RocknRoll", "Gun", "Thumbs up" };

        private AnimatorController fxController;

        private int layerIndex = 0;

        public void Initialize(VRCAvatar editAvatar,
                               VRCAvatar originalAvatar,
                               string saveFolderPath,
                               VRCAvatarEditorGUI vrcAvatarEditorGUI,
                               FaceEmotionGUI3 faceEmotionGUI)
        {
            this.editAvatar = editAvatar;
            this.originalAvatar = originalAvatar;
            this.vrcAvatarEditorGUI = vrcAvatarEditorGUI;
            this.faceEmotionGUI = faceEmotionGUI;

            Initialize(saveFolderPath);

            if (editAvatar != null && editAvatar.FxController != null)
            {
                // TODO: AnimationClipのバリデーション機能
            }
        }

        public override bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, layoutOptions))
            {
                if (originalAvatar != null)
                {
                    fxController = originalAvatar.FxController;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("FX Layer", EditorStyles.boldLabel);

                    // TODO: Emote一覧に切り替えるボタン
                }

                EditorGUILayout.Space();

                // TODO: Emote一覧と切り替えられるように？
                if (fxController != null)
                {
                    var layerNames = fxController.layers.Select(l => l.name).ToArray();
                    editAvatar.TargetFxLayerIndex = EditorGUILayout.Popup("Layer", editAvatar.TargetFxLayerIndex, layerNames);
                    var states = fxController.layers[editAvatar.TargetFxLayerIndex]
                                    .stateMachine.states
                                    .Where(s => !(s.state.motion is BlendTree))
                                    .OrderBy(s => s.state.name)
                                    .ToArray();

                    GatoGUILayout.ErrorBox(LocalizeText.instance.langPair.haveNoAnimationClipMessageText, states.Any(), MessageType.Info);

                    AnimationClip anim;
                    for (int i = 0; i < states.Length; i++)
                    {
                        var stateName = states[i].state.name;
                        anim = states[i].state.motion as AnimationClip;

                        states[i].state.motion = EdittableAnimationField(
                            $"{i + 1}:{stateName}",
                            anim,
                            false,
                            anim != null && !anim.name.StartsWith("proxy_"),
                            () => {
                                if (vrcAvatarEditorGUI.CurrentTool != VRCAvatarEditorGUI.ToolFunc.FaceEmotion)
                                {
                                    vrcAvatarEditorGUI.CurrentTool = VRCAvatarEditorGUI.ToolFunc.FaceEmotion;
                                }
                                FaceEmotion.ApplyAnimationProperties(anim, editAvatar);
                                ((FaceEmotionGUI3)faceEmotionGUI).ChangeSaveAnimationState(
                                    anim.name,
                                    i,
                                    anim);
                            });
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(LocalizeText.instance.langPair.missingFxControllerMessageText, MessageType.Warning);

                    if (GUILayout.Button(LocalizeText.instance.langPair.createFxControllerText))
                    {
                        CreatePlayableLayerController(originalAvatar, editAvatar);
                        vrcAvatarEditorGUI.OnToolChanged();
                    }
                }
            }

            return false;
        }

        private static AnimatorController InstantiateFxController(string newFilePath)
        {
            string path = VRCSDKUtility.GetVRCSDKFilePath("vrc_AvatarV3HandsLayer");

            newFilePath = AssetDatabase.GenerateUniqueAssetPath(newFilePath);
            AssetDatabase.CopyAsset(path, newFilePath);
            var controller = AssetDatabase.LoadAssetAtPath(newFilePath, typeof(AnimatorController)) as AnimatorController;

            return controller;
        }

        private static void EnableCustomPlayableLayers(VRCAvatar avatar)
        {
            avatar.Descriptor.customizeAnimationLayers = true;
        }

        public static void CreateGestureController(VRCAvatar originalAvatar, VRCAvatar editAvatar)
        {
            if (!originalAvatar.Descriptor.customizeAnimationLayers)
            {
                EnableCustomPlayableLayers(originalAvatar);
                EnableCustomPlayableLayers(editAvatar);
            }

            if (originalAvatar.GestureController is null)
            {
                string saveFolderPath;
                if (originalAvatar.FxController != null)
                {
                    saveFolderPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(originalAvatar.FxController));
                }
                else
                {
                    saveFolderPath = "Assets/" + originalAvatar.Animator.gameObject.name + "/";
                }

                var fileName = $"Gesture_HandsLayer_{ originalAvatar.Animator.gameObject.name}.controller";
                var createdGestureController = InstantiateFxController(Path.Combine(saveFolderPath, fileName));

                originalAvatar.Descriptor.baseAnimationLayers[2].isDefault = false;
                editAvatar.Descriptor.baseAnimationLayers[2].isDefault = false;
                originalAvatar.Descriptor.baseAnimationLayers[2].animatorController = createdGestureController;
                editAvatar.Descriptor.baseAnimationLayers[2].animatorController = createdGestureController;
            }

            originalAvatar.LoadAvatarInfo();
            editAvatar.LoadAvatarInfo();
        }

        public static void CreatePlayableLayerController(VRCAvatar originalAvatar, VRCAvatar editAvatar)
        {
            var fileName = $"Fx_HandsLayer_{ originalAvatar.Animator.gameObject.name}.controller";
            var saveFolderPath = "Assets/" + originalAvatar.Animator.gameObject.name + "/";
            var fullFolderPath = Path.GetFullPath(saveFolderPath);
            if (!Directory.Exists(fullFolderPath))
            {
                Directory.CreateDirectory(fullFolderPath);
                AssetDatabase.Refresh();
            }
            var createdFxController = InstantiateFxController(Path.Combine(saveFolderPath, fileName));

            // まばたき防止機構をつける
            SetNoBlink(createdFxController);

            if (!originalAvatar.Descriptor.customizeAnimationLayers)
            {
                EnableCustomPlayableLayers(originalAvatar);
                EnableCustomPlayableLayers(editAvatar);
            }
            originalAvatar.Descriptor.baseAnimationLayers[4].isDefault = false;
            originalAvatar.Descriptor.baseAnimationLayers[4].animatorController = createdFxController;
            editAvatar.Descriptor.baseAnimationLayers[4].isDefault = false;
            editAvatar.Descriptor.baseAnimationLayers[4].animatorController = createdFxController;

            if (originalAvatar.GestureController is null)
            {
                fileName = $"Gesture_HandsLayer_{ originalAvatar.Animator.gameObject.name}.controller";
                var createdGestureController = InstantiateFxController(Path.Combine(saveFolderPath, fileName));

                originalAvatar.Descriptor.baseAnimationLayers[2].isDefault = false;
                originalAvatar.Descriptor.baseAnimationLayers[2].animatorController = createdGestureController;
                editAvatar.Descriptor.baseAnimationLayers[2].isDefault = false;
                editAvatar.Descriptor.baseAnimationLayers[2].animatorController = createdGestureController;
            }

            originalAvatar.LoadAvatarInfo();
            editAvatar.LoadAvatarInfo();
        }

        private static void SetNoBlink(AnimatorController fxController)
        {
            var layers = fxController.layers.Where(l => l.name == "Left Hand" || l.name == "Right Hand");
            foreach (var layer in layers)
            {
                var states = layer.stateMachine.states;

                foreach (var state in states)
                {
                    var stateName = state.state.name;
                    if (!EMOTIONSTATES.Contains(stateName)) continue;

                    var control = state.state.AddStateMachineBehaviour(typeof(VRCAnimatorTrackingControl)) as VRCAnimatorTrackingControl;

                    if (stateName == "Idle")
                    {
                        control.trackingEyes = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;
                    }
                    else
                    {
                        control.trackingEyes = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
                    }
                }
            }
        }
    }
}
#endif