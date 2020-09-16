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

namespace VRCAvatarEditor.Avatars3
{
    public class FaceEmotionGUI3 : FaceEmotionGUIBase
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
                    // TODO: 日本語対応
                    EditorGUILayout.HelpBox("Create Only (not set to AnimatorController) because exist no states in this layer.", MessageType.Info);
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
                        // TODO: 日本語対応
                        EditorGUILayout.HelpBox("HandPose Animation can't be chaged because not found target layer or state.", MessageType.Info);
                    }
                }
                else
                {
                    // TODO: 日本語対応
                    EditorGUILayout.HelpBox("No Gesture Layer Controller", MessageType.Warning);

                    GatoGUILayout.Button(
                        "Create Gesture Layer Controller",
                        () =>
                        {
                            AnimationsGUI.CreateGestureController(originalAvatar, editAvatar);
                            parentWindow.OnTabChanged();
                        });
                }

                // TODO: 日本語対応
                setLeftAndRight = EditorGUILayout.ToggleLeft("Set to Left & Right Hand Layer", setLeftAndRight);
            }
            else
            {
                // TODO: 日本語対応
                EditorGUILayout.HelpBox("No Fx Layer Controller", MessageType.Error);

                GatoGUILayout.Button(
                    "Create Fx Layer Controller",
                    () =>
                    {
                        AnimationsGUI.CreatePlayableLayerController(originalAvatar, editAvatar);
                        parentWindow.OnTabChanged();
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
                                           editAvatar);

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
                           if (layerName == "Left Hand")
                           {
                               targetLayerName = "Right Hand";
                           }
                           else if (layerName == "Right Hand")
                           {
                               targetLayerName = "Left Hand";
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
                   }

                   FaceEmotion.ResetToDefaultFaceEmotion(editAvatar);
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