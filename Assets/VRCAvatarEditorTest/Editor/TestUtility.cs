using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRCAvatarEditor.Test
{
    public static class TestUtility
    {
        private const string TEST_FOLDER_PATH = "Assets/VRCAvatarEditorTest/Editor";
        private static readonly string TEST_SCENE_PATH = Path.Combine(TEST_FOLDER_PATH, "TestScene.unity");
        private static readonly string TEST_AVATARS_ASSET_PATH = Path.Combine(TEST_FOLDER_PATH, "TestAvatars.asset");

        public static Scene OpenTestScene()
        {
            return EditorSceneManager.OpenScene(
                TEST_SCENE_PATH,
                OpenSceneMode.Single
            );
        }

        public static TestAvatars GetTestAvatars()
        {
            return AssetDatabase.LoadAssetAtPath<TestAvatars>(
                TEST_AVATARS_ASSET_PATH
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
