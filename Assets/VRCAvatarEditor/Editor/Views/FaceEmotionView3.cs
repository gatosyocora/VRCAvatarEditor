#if VRC_SDK_VRCSDK3
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRCAvatarEditor.Base;
using VRCAvatar = VRCAvatarEditor.Avatars3.VRCAvatar3;
using AnimationsGUI = VRCAvatarEditor.Avatars3.AnimationsView3;
using VRCAvatarEditor.Utilities;

namespace VRCAvatarEditor.Avatars3
{
    public class FaceEmotionView3 : FaceEmotionViewBase
    {
        private int selectedStateIndex = 0;
        private bool setLeftAndRight = true;

        private ChildAnimatorState[] states;

        protected override void DrawCreatedAnimationSettingsGUI()
        {
            base.DrawCreatedAnimationSettingsGUI();

            string[] stateNames;

            if (editAvatar.FxController != null)
            {
                states = VRCAvatarAnimationUtility.GetStates(editAvatar.FxController.layers[editAvatar.TargetFxLayerIndex]);
                stateNames = states.Select((s, i) => $"{i + 1}:{s.state.name}").ToArray();

                EditorGUILayout.LabelField("Layer", editAvatar.FxController.layers[editAvatar.TargetFxLayerIndex].name);

                // Stateがないとき、自動設定できない
                if (states.Any())
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        selectedStateIndex = EditorGUILayout.Popup(
                            "State",
                            selectedStateIndex,
                            stateNames);

                        if (check.changed)
                        {
                            // TODO: 手のアニメーションファイルを変更する？
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(LocalizeText.instance.langPair.createOnlyMessageText, MessageType.Info);
                }

                if (editAvatar.GestureController != null)
                {
                    ChildAnimatorState handState = default;
                    if (states.Any())
                    {
                        var targetLayer = editAvatar.GestureController.layers
                                            .Where(l => l.name == editAvatar.FxController.layers[editAvatar.TargetFxLayerIndex].name)
                                            .SingleOrDefault();

                        if (targetLayer != null)
                        {
                            handState = targetLayer
                                            .stateMachine.states
                                            .Where(s => !(s.state.motion is BlendTree) &&
                                                        s.state.name == states[selectedStateIndex].state.name)
                                            .SingleOrDefault();
                        }
                    }

                    // LayerまたはStateが見つからない時はGestureまわりは利用できない
                    if (handState.state != null)
                    {
                        handPoseAnim = handState.state.motion as AnimationClip;
                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            handPoseAnim = GatoGUILayout.ObjectField(
                                LocalizeText.instance.langPair.handPoseAnimClipLabel,
                                handPoseAnim);

                            if (check.changed)
                            {
                                handState.state.motion = handPoseAnim;
                                EditorUtility.SetDirty(editAvatar.GestureController);
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(LocalizeText.instance.langPair.handPoseChangeFailedMessageText, MessageType.Info);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(LocalizeText.instance.langPair.missingGestureControllerMessageText, MessageType.Warning);

                    GatoGUILayout.Button(
                        LocalizeText.instance.langPair.createGestureControllerText,
                        () =>
                        {
                            AnimationsGUI.CreateGestureController(originalAvatar, editAvatar);
                            parentWindow.OnToolChanged();
                        });
                }

                setLeftAndRight = EditorGUILayout.ToggleLeft(LocalizeText.instance.langPair.setLeftAndRightHandLayerText, setLeftAndRight);
            }
            else
            {
                EditorGUILayout.HelpBox(LocalizeText.instance.langPair.missingFxControllerMessageText, MessageType.Error);

                GatoGUILayout.Button(
                    LocalizeText.instance.langPair.createFxControllerText,
                    () =>
                    {
                        AnimationsGUI.CreatePlayableLayerController(originalAvatar, editAvatar);
                        parentWindow.OnToolChanged();
                    });
            }
        }

        protected override void DrawCreateButtonGUI()
        {
            GatoGUILayout.Button(
               LocalizeText.instance.langPair.createAnimFileButtonText,
               () => {
                   var targetState = states.Any() ? states[selectedStateIndex].state : null;
                   OnCreateButtonClicked(originalAvatar, editAvatar, animName, targetState, setLeftAndRight);
               },
               originalAvatar.FxController != null);
        }

        public override void OnLoadedAnimationProperties()
        {
            base.OnLoadedAnimationProperties();
            ChangeSaveAnimationState();
        }

        public override void OnSetToDefaultButtonClick(VRCAvatar editAvatar, VRCAvatar originalAvatar)
        {
            base.OnSetToDefaultButtonClick(editAvatar, originalAvatar);

            // DefaultFaceレイヤーを使っているときにそのAnimationファイルも更新する
            if (originalAvatar.FxController.layers.Any(l => l.name == VRCAvatarConstants.RESET_LAYER_NAME))
            {
                var defaultLayer = originalAvatar.FxController.layers
                                        .SingleOrDefault(l => l.name == VRCAvatarConstants.RESET_LAYER_NAME);

                if (defaultLayer == null)
                {
                    Debug.LogError("Not Found Default Face Layer");
                    return;
                }

                var targetState = defaultLayer.stateMachine.states
                                    .SingleOrDefault(s => s.state.name == VRCAvatarConstants.RESET_FACE_STATE_NAME);

                if (targetState.state == null)
                {
                    Debug.LogError("Not Found Default Face State");
                    return;
                }

                var defaultFaceAnimation = FaceEmotion.CreateBlendShapeAnimationClip(
                                                VRCAvatarConstants.DEFAULT_FACE_ANIMATION_NAME,
                                                originalAvatar.AnimSavedFolderPath,
                                                editAvatar);

                targetState.state.motion = defaultFaceAnimation;
                EditorUtility.SetDirty(originalAvatar.FxController);
            }
        }

        public override void ChangeSaveAnimationState()
        {
            ChangeSaveAnimationState("", 0, null);
        }

        private void OnCreateButtonClicked(VRCAvatar originalAvatar, VRCAvatar editAvatar, string animName, AnimatorState state, bool setLeftAndRight)
        {
            var controller = originalAvatar.FxController;

            var createdAnimClip = FaceEmotion.CreateBlendShapeAnimationClip(
                                    animName,
                                    originalAvatar.AnimSavedFolderPath,
                                    editAvatar);

            // Stateがない場合は作成のみ
            if (state != null)
            {
                state.motion = createdAnimClip;
                EditorUtility.SetDirty(controller);

                // 可能であればもう一方の手も同じAnimationClipを設定する
                if (setLeftAndRight)
                {
                    var layerName = editAvatar.FxController.layers[editAvatar.TargetFxLayerIndex].name;
                    string targetLayerName = string.Empty;
                    if (layerName == VRCAvatarConstants.FX_LEFT_HAND_LAYER_NAME)
                    {
                        targetLayerName = VRCAvatarConstants.FX_RIGHT_HAND_LAYER_NAME;
                    }
                    else if (layerName == VRCAvatarConstants.FX_RIGHT_HAND_LAYER_NAME)
                    {
                        targetLayerName = VRCAvatarConstants.FX_LEFT_HAND_LAYER_NAME;
                    }

                    if (!string.IsNullOrEmpty(targetLayerName))
                    {
                        var targetLayer = editAvatar.FxController.layers
                                            .Where(l => l.name == targetLayerName)
                                            .SingleOrDefault();

                        if (targetLayer != null)
                        {
                            var targetStateName = state.name;
                            var targetState = targetLayer.stateMachine.states
                                                .Where(s => s.state.name == targetStateName)
                                                .SingleOrDefault();

                            if (targetState.state != null)
                            {
                                targetState.state.motion = createdAnimClip;
                                EditorUtility.SetDirty(controller);
                            }
                        }
                    }
                }
            }
            else
            {
                FaceEmotion.ResetToDefaultFaceEmotion(editAvatar);
            }
            originalAvatar.FxController = controller;
            editAvatar.FxController = controller;
        }

        public void ChangeSaveAnimationState(
                string animName = "",
                int selectStateIndex = 0,
                AnimationClip handPoseAnim = null)
        {
            this.animName = animName;
            // TODO: 一時対処
            this.selectedHandAnim = HandPose.HandPoseType.NoSelection;
            if (handPoseAnim is null)
                handPoseAnim = HandPose.GetHandAnimationClip(this.selectedHandAnim);
            this.handPoseAnim = handPoseAnim;
        }

        private void ChangeSelectionHandAnimation()
        {
            if (usePreviousAnimationOnHandAnimation)
            {
                // TODO: 以前のアニメーションの取得
            }
            else
            {
                handPoseAnim = HandPose.GetHandAnimationClip(selectedHandAnim);
            }
        }

        private void SetupForNoUseWriteDefaultIfNeeded(AnimatorController controller, VRCAvatar originalAvatar, VRCAvatar editAvatar)
        {
            if (!VRCAvatarAnimationUtility.UseWriteDefaults(controller))
            {
                if (!VRCAvatarAnimationUtility.ExistLayer(controller, VRCAvatarConstants.RESET_LAYER_NAME))
                {
                    VRCAvatarAnimationUtility.AddDefaultFaceLayer(controller, originalAvatar, editAvatar);
                    editAvatar.TargetFxLayerIndex++;
                }

                // Idleステートに何かしらが入っていないとバグるので対策
                var fxLeftHandIdleState = VRCAvatarAnimationUtility.GetFXLayerIdleState(editAvatar.FxController, VRCAvatarAnimationUtility.HandType.LEFT);
                var fxRightHandIdleState = VRCAvatarAnimationUtility.GetFXLayerIdleState(editAvatar.FxController, VRCAvatarAnimationUtility.HandType.RIGHT);
                if (fxLeftHandIdleState.state.motion == null || fxRightHandIdleState.state.motion == null)
                {
                    var emptyAnimation = VRCAvatarAnimationUtility.GetOrCreateEmptyAnimation(originalAvatar);
                    if (fxLeftHandIdleState.state.motion == null)
                    {
                        fxLeftHandIdleState.state.motion = emptyAnimation;
                    }
                    if (fxRightHandIdleState.state.motion == null)
                    {
                        fxRightHandIdleState.state.motion = emptyAnimation;
                    }
                    EditorUtility.SetDirty(originalAvatar.FxController);
                    EditorUtility.SetDirty(editAvatar.FxController);
                }
            }
        }
    }
}
#endif