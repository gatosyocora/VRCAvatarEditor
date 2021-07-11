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
using AnimationsGUI = VRCAvatarEditor.Avatars3.AnimationsGUI3;
using VRCAvatarEditor.Utilities;

namespace VRCAvatarEditor.Avatars3
{
    public class FaceEmotionGUI3 : FaceEmotionGUIBase
    {
        private int selectedStateIndex = 0;
        private bool setLeftAndRight = true;

        private ChildAnimatorState[] states;

        private const string FX_LEFT_HAND_LAYER_NAME = "Left Hand";
        private const string FX_RIGHT_HAND_LAYER_NAME = "Right Hand";
        private const string FX_DEFAULT_LAYER_NAME = "DefaultFace";

        protected override void DrawCreatedAnimationSettingsGUI()
        {
            base.DrawCreatedAnimationSettingsGUI();

            string[] stateNames;

            if (editAvatar.FxController != null)
            {
                var stateMachine = editAvatar.FxController.layers[editAvatar.TargetFxLayerIndex].stateMachine;
                states = stateMachine.states
                            .Where(s => !(s.state.motion is BlendTree))
                            .OrderBy(s => s.state.name)
                            .ToArray();
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
                    if (editAvatar.GestureController.layers.Length > editAvatar.TargetFxLayerIndex &&
                        states.Any())
                    {
                        handState = editAvatar.GestureController.layers[editAvatar.TargetFxLayerIndex]
                                        .stateMachine.states
                                        .Where(s => !(s.state.motion is BlendTree) &&
                                                    s.state.name == states[selectedStateIndex].state.name)
                                        .SingleOrDefault();
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
               () =>
               {
                   var controller = originalAvatar.FxController;

                   var createdAnimClip = FaceEmotion.CreateBlendShapeAnimationClip(
                                           animName,
                                           originalAvatar.AnimSavedFolderPath,
                                           editAvatar,
                                           true);

                   // Stateがない場合は作成のみ
                   if (states.Any())
                   {
                       states[selectedStateIndex].state.motion = createdAnimClip;
                       EditorUtility.SetDirty(controller);

                        // 可能であればもう一方の手も同じAnimationClipを設定する
                        if (setLeftAndRight)
                       {
                           var layerName = editAvatar.FxController.layers[editAvatar.TargetFxLayerIndex].name;
                           string targetLayerName = string.Empty;
                           if (layerName == FX_LEFT_HAND_LAYER_NAME)
                           {
                               targetLayerName = FX_RIGHT_HAND_LAYER_NAME;
                           }
                           else if (layerName == FX_RIGHT_HAND_LAYER_NAME)
                           {
                               targetLayerName = FX_LEFT_HAND_LAYER_NAME;
                           }

                           if (!string.IsNullOrEmpty(targetLayerName))
                           {
                               var targetLayer = editAvatar.FxController.layers
                                                   .Where(l => l.name == targetLayerName)
                                                   .SingleOrDefault();

                               if (targetLayer != null)
                               {
                                   var targetStateName = states[selectedStateIndex].state.name;
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

                       // WriteDefaultオフで表情が戻らなくなる不具合の対策
                       FaceEmotion.ResetToDefaultFaceEmotion(editAvatar);
                       if (!VRCAvatarAnimationUtility.UseWriteDefaults(controller))
                       {
                           if (!VRCAvatarAnimationUtility.ExistLayer(controller, FX_DEFAULT_LAYER_NAME))
                           {
                               var defaultLayer = VRCAvatarAnimationUtility.InsertLayer(controller, 1, FX_DEFAULT_LAYER_NAME);
                               var defaultState = defaultLayer.stateMachine.AddState("Reset");
                               defaultState.writeDefaultValues = false;
                               var defaultFaceAnimation = FaceEmotion.CreateBlendShapeAnimationClip(
                                                           "DefaultFace",
                                                           originalAvatar.AnimSavedFolderPath,
                                                           editAvatar,
                                                           true);
                               defaultState.motion = defaultFaceAnimation;
                               EditorUtility.SetDirty(controller);
                           }

                           // Idleステートに何かしらが入っていないとバグるので対策
                           var fxLeftHandIdleState = editAvatar.FxController.layers
                                                        .Where(l => l.name == FX_LEFT_HAND_LAYER_NAME)
                                                        .SelectMany(l => l.stateMachine.states)
                                                        .Where(s => s.state.name == VRCAvatarAnimationUtility.IDLE_STATE_NAME)
                                                        .SingleOrDefault();
                           var fxRightHandIdleState = editAvatar.FxController.layers
                                                         .Where(l => l.name == FX_RIGHT_HAND_LAYER_NAME)
                                                         .SelectMany(l => l.stateMachine.states)
                                                         .Where(s => s.state.name == VRCAvatarAnimationUtility.IDLE_STATE_NAME)
                                                         .SingleOrDefault();
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
                   } else
                   {
                       FaceEmotion.ResetToDefaultFaceEmotion(editAvatar);
                   }
                   originalAvatar.FxController = controller;
                   editAvatar.FxController = controller;
               },
               originalAvatar.FxController != null);
        }

        public override void OnLoadedAnimationProperties()
        {
            base.OnLoadedAnimationProperties();
            ChangeSaveAnimationState();
        }

        public override void ChangeSaveAnimationState()
        {
            ChangeSaveAnimationState("", 0, null);
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
    }
}
#endif