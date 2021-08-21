using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRCAvatarEditor.Test
{
    public static class TestUtility
    {
        public static Scene OpenTestScene()
        {
            return EditorSceneManager.OpenScene(
                "Assets/VRCAvatarEditorTest/Editor/TestScene.unity",
                OpenSceneMode.Single
            );
        }

        public static TestAvatars GetTestAvatars()
        {
            return AssetDatabase.LoadAssetAtPath<TestAvatars>(
                "Assets/VRCAvatarEditorTest/Editor/TestAvatars.asset"
            );
        }

        public static T ResetComponent<T>(GameObject gameObject) where T : MonoBehaviour {
            var behaviour = gameObject.GetComponent<T>();
            if (behaviour == null) return null;
            Object.DestroyImmediate(behaviour);
            return gameObject.AddComponent<T>();
        }
    }
}
