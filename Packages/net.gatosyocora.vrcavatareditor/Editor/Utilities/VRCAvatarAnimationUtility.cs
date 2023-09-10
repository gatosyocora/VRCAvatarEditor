using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRCAvatarEditor.Base;
using VRCAvatarEditor.Avatars3;
using VRC.SDK3.Avatars.Components;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;

namespace VRCAvatarEditor.Utilities
{
    public class VRCAvatarAnimationUtility
    {
        public enum HandType
        {
            LEFT, RIGHT
        }

        public static bool UseWriteDefaults(AnimatorController controller)
        {
            return controller.layers.Select(layer => layer.stateMachine)
                    .SelectMany(stateMachine => stateMachine.states)
                    .Any(state => state.state.writeDefaultValues);
        }

        public static AnimatorControllerLayer InsertLayer(AnimatorController controller, int index, string layerName)
        {
            controller.AddLayer(layerName);
            var layers = controller.layers;
            var movedLayer = layers.Last();
            movedLayer.defaultWeight = 1;
            for (int i = index; i < layers.Length; i++)
            {
                var currentLayer = layers[i];
                layers[i] = movedLayer;
                movedLayer = currentLayer;
            }
            controller.layers = layers;
            EditorUtility.SetDirty(controller);
            return movedLayer;
        }

        public static bool ExistLayer(AnimatorController controller, string layerName)
            => controller.layers.Any(layer => layer.name == layerName);

        public static AnimatorState AddState(AnimatorStateMachine stateMachine, string stateName, bool useWriteDefault) {
            var state = stateMachine.AddState(stateName);
            state.writeDefaultValues = useWriteDefault;
            return state;
        }

        public static AnimationClip GetOrCreateEmptyAnimation(VRCAvatarBase avatar)
        {
            var animationFilePath = Path.Combine(avatar.AnimSavedFolderPath, $"{VRCAvatarConstants.EMPTY_ANIMATION_NAME}.anim");
            var emptyAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationFilePath);
            if (emptyAnimation != null) return emptyAnimation;

            emptyAnimation = new AnimationClip
            {
                name = VRCAvatarConstants.EMPTY_ANIMATION_NAME
            };
            AssetDatabase.CreateAsset(emptyAnimation, animationFilePath);
            AssetDatabase.SaveAssets();
            return emptyAnimation;
        }

        public static void AddDefaultFaceLayer(AnimatorController controller, VRCAvatarBase originalAvatar, VRCAvatarBase editAvatar)
        {
            var defaultLayer = InsertLayer(controller, 1, VRCAvatarConstants.RESET_LAYER_NAME);
            var defaultState = AddState(defaultLayer.stateMachine, VRCAvatarConstants.RESET_FACE_STATE_NAME, false);
            var defaultFaceAnimation = new FaceEmotion().CreateBlendShapeAnimationClip(
                                        VRCAvatarConstants.DEFAULT_FACE_ANIMATION_NAME,
                                        originalAvatar.AnimSavedFolderPath,
                                        editAvatar);
            defaultState.motion = defaultFaceAnimation;
            EditorUtility.SetDirty(controller);
        }

        public static ChildAnimatorState[] GetStates(AnimatorControllerLayer layer)
            => layer.stateMachine.states
                    .Where(s => !(s.state.motion is BlendTree))
                    .Where(s => s.state.name != VRCAvatarConstants.IDLE_STATE_NAME)
                    .OrderBy(s => s.state.name)
                    .ToArray();

        public static AnimatorControllerLayer GetLayerWithHandChanged(AnimatorController controller, HandType handType)
        {
            var layerNames = handType == HandType.LEFT ? VRCAvatarConstants.FX_LEFT_HAND_LAYER_NAME_PATTERNS : VRCAvatarConstants.FX_RIGHT_HAND_LAYER_NAME_PATTERNS;
            return controller.layers.SingleOrDefault(l => layerNames.Contains(l.name));
        }

        public static ChildAnimatorState GetFXLayerIdleState(AnimatorController controller, HandType handType)
            => GetLayerWithHandChanged(controller, handType)
                .stateMachine.states
                .SingleOrDefault(s => s.state.name == VRCAvatarConstants.IDLE_STATE_NAME);

        public static CustomAnimLayer GetPlayableLayer(VRCAvatarDescriptor descripter, AnimLayerType layerType)
            => descripter.baseAnimationLayers
                .Where(l => l.type == layerType)
                .FirstOrDefault(l => l.animatorController != null);

        public static AnimatorControllerLayer DetectCorrespondenceLayer(AnimatorControllerLayer layer, AnimatorController controller)
        {
            var layerName = layer.name;
            string targetLayerName = string.Empty;
            if (layerName == VRCAvatarConstants.FX_LEFT_HAND_LAYER_NAME)
            {
                targetLayerName = VRCAvatarConstants.FX_RIGHT_HAND_LAYER_NAME;
            }
            else if (layerName == VRCAvatarConstants.FX_RIGHT_HAND_LAYER_NAME)
            {
                targetLayerName = VRCAvatarConstants.FX_LEFT_HAND_LAYER_NAME;
            }

            if (string.IsNullOrEmpty(targetLayerName)) return null;

            return controller.layers
                    .Where(l => l.name == targetLayerName)
                    .SingleOrDefault();
        }
    }
}
