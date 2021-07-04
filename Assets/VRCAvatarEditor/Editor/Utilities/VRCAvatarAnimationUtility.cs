using System.Linq;
using UnityEditor;
using UnityEditor.Animations;

namespace VRCAvatarEditor.Utilities
{
    public class VRCAvatarAnimationUtility
    {
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
    }
}
