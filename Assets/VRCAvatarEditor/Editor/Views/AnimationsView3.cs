#if VRC_SDK_VRCSDK3
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
    public class AnimationsView3 : AnimationsViewBase
    {
        private VRCAvatar editAvatar;
        private VRCAvatar originalAvatar;

        public static readonly string[] EMOTIONSTATES = { "Idle", "Fist", "Open", "Point", "Peace", "RockNRoll", "Gun", "Thumbs up" };

        private AnimatorController fxController;

        private const int PLAYABLE_GESTURE_LAYER_INDEX = 2;
        private const int PLAYABLE_FX_LAYER_INDEX = 4;

        public void Initialize(VRCAvatar editAvatar,
                               VRCAvatar originalAvatar,
                               string saveFolderPath,
                               VRCAvatarEditorView vrcAvatarEditorGUI,
                               FaceEmotionView3 faceEmotionGUI)
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
                    var states = VRCAvatarAnimationUtility.GetStates(fxController.layers[editAvatar.TargetFxLayerIndex]);

                    GatoGUILayout.ErrorBox(LocalizeText.instance.langPair.haveNoAnimationClipMessageText, states.Any(), MessageType.Info);

                    AnimationClip anim;
                    for (int i = 0; i < states.Length; i++)
                    {
                        anim = states[i].state.motion as AnimationClip;

                        states[i].state.motion = EdittableAnimationField(
                            $"{i + 1}:{states[i].state.name}",
                            anim,
                            false,
                            anim != null && !anim.name.StartsWith(VRCAvatarConstants.OFFICIAL_ANIMATION_PREFIX),
                            () => {
                                if (vrcAvatarEditorGUI.CurrentTool != VRCAvatarEditorView.ToolFunc.FaceEmotion)
                                {
                                    vrcAvatarEditorGUI.CurrentTool = VRCAvatarEditorView.ToolFunc.FaceEmotion;
                                }
                                new FaceEmotion().ApplyAnimationProperties(anim, editAvatar);
                                ((FaceEmotionView3)faceEmotionGUI).ChangeSaveAnimationState(anim.name, i, anim);
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
            string path = VRCSDKUtility.GetVRCSDKFilePath("Animation/Controllers/vrc_AvatarV3HandsLayer.controller");

            newFilePath = AssetDatabase.GenerateUniqueAssetPath(newFilePath);
            AssetDatabase.CopyAsset(path, newFilePath);
            var controller = AssetDatabase.LoadAssetAtPath(newFilePath, typeof(AnimatorController)) as AnimatorController;

            return controller;
        }

        private static void EnableCustomPlayableLayers(VRCAvatar avatar)
        {
            var baseLayerTypes = new VRCAvatarDescriptor.AnimLayerType[]
            {
                VRCAvatarDescriptor.AnimLayerType.Base,
                VRCAvatarDescriptor.AnimLayerType.Additive,
                VRCAvatarDescriptor.AnimLayerType.Gesture,
                VRCAvatarDescriptor.AnimLayerType.Action,
                VRCAvatarDescriptor.AnimLayerType.FX
            };

            var specialLayerTypes = new VRCAvatarDescriptor.AnimLayerType[]
            {
                VRCAvatarDescriptor.AnimLayerType.Sitting,
                VRCAvatarDescriptor.AnimLayerType.TPose,
                VRCAvatarDescriptor.AnimLayerType.IKPose
            };

            avatar.Descriptor.customizeAnimationLayers = true;
            avatar.Descriptor.baseAnimationLayers = baseLayerTypes
                                                        .Select(type => new VRCAvatarDescriptor.CustomAnimLayer
                                                        {
                                                            type = type
                                                        })
                                                        .ToArray();
            avatar.Descriptor.specialAnimationLayers = specialLayerTypes
                                                        .Select(type => new VRCAvatarDescriptor.CustomAnimLayer
                                                        {
                                                            type = type
                                                        })
                                                        .ToArray();
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

                originalAvatar.Descriptor.baseAnimationLayers[PLAYABLE_GESTURE_LAYER_INDEX].isDefault = false;
                editAvatar.Descriptor.baseAnimationLayers[PLAYABLE_GESTURE_LAYER_INDEX].isDefault = false;
                originalAvatar.Descriptor.baseAnimationLayers[PLAYABLE_GESTURE_LAYER_INDEX].animatorController = createdGestureController;
                editAvatar.Descriptor.baseAnimationLayers[PLAYABLE_GESTURE_LAYER_INDEX].animatorController = createdGestureController;
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

            // WriteDefaultsがオフによる不具合を防止する機構をつける
            if (!VRCAvatarAnimationUtility.UseWriteDefaults(createdFxController))
            {
                if (!VRCAvatarAnimationUtility.ExistLayer(createdFxController, VRCAvatarConstants.RESET_LAYER_NAME))
                {
                    VRCAvatarAnimationUtility.AddDefaultFaceLayer(createdFxController, originalAvatar, editAvatar);
                    editAvatar.TargetFxLayerIndex++;
                }
            }

            if (!originalAvatar.Descriptor.customizeAnimationLayers)
            {
                EnableCustomPlayableLayers(originalAvatar);
                EnableCustomPlayableLayers(editAvatar);
            }
            originalAvatar.Descriptor.baseAnimationLayers[PLAYABLE_FX_LAYER_INDEX].isDefault = false;
            originalAvatar.Descriptor.baseAnimationLayers[PLAYABLE_FX_LAYER_INDEX].animatorController = createdFxController;
            editAvatar.Descriptor.baseAnimationLayers[PLAYABLE_FX_LAYER_INDEX].isDefault = false;
            editAvatar.Descriptor.baseAnimationLayers[PLAYABLE_FX_LAYER_INDEX].animatorController = createdFxController;

            if (originalAvatar.GestureController is null)
            {
                fileName = $"Gesture_HandsLayer_{ originalAvatar.Animator.gameObject.name}.controller";
                var createdGestureController = InstantiateFxController(Path.Combine(saveFolderPath, fileName));

                originalAvatar.Descriptor.baseAnimationLayers[PLAYABLE_GESTURE_LAYER_INDEX].isDefault = false;
                originalAvatar.Descriptor.baseAnimationLayers[PLAYABLE_GESTURE_LAYER_INDEX].animatorController = createdGestureController;
                editAvatar.Descriptor.baseAnimationLayers[PLAYABLE_GESTURE_LAYER_INDEX].isDefault = false;
                editAvatar.Descriptor.baseAnimationLayers[PLAYABLE_GESTURE_LAYER_INDEX].animatorController = createdGestureController;
            }

            originalAvatar.LoadAvatarInfo();
            editAvatar.LoadAvatarInfo();
        }

        private static void SetNoBlink(AnimatorController fxController)
        {
            var layers = fxController.layers.Where(
                            l => l.name == VRCAvatarConstants.FX_LEFT_HAND_LAYER_NAME || 
                            l.name == VRCAvatarConstants.FX_RIGHT_HAND_LAYER_NAME);
            foreach (var layer in layers)
            {
                var states = layer.stateMachine.states;

                foreach (var state in states)
                {
                    var stateName = state.state.name;
                    if (!EMOTIONSTATES.Contains(stateName)) continue;

                    var control = state.state.AddStateMachineBehaviour(typeof(VRCAnimatorTrackingControl)) as VRCAnimatorTrackingControl;

                    if (stateName == VRCAvatarConstants.IDLE_STATE_NAME)
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