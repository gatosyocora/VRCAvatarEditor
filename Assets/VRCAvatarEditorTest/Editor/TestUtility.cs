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
        private const string CACHE_FOLDER_NAME = "Cache";
        public static readonly string CACHE_FOLDER_PATH = Path.Combine(TEST_FOLDER_PATH, CACHE_FOLDER_NAME);
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

        public static string CreateCacheFolder()
        {
            if (AssetDatabase.IsValidFolder(CACHE_FOLDER_PATH))
            {
                return CACHE_FOLDER_PATH;
            }

            AssetDatabase.CreateFolder(TEST_FOLDER_PATH, CACHE_FOLDER_NAME);
            return CACHE_FOLDER_PATH;
        }

        public static void DeleteCache()
        {
            AssetDatabase.DeleteAsset(CACHE_FOLDER_PATH);
        }

        public static T DuplicateAssetToCache<T>(Object asset) where T : Object
        {
            if (!AssetDatabase.IsValidFolder(CACHE_FOLDER_PATH))
            {
                CreateCacheFolder();
            }

            var assetPath = AssetDatabase.GetAssetPath(asset);
            var newPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(CACHE_FOLDER_PATH, Path.GetFileName(assetPath))
            );
            AssetDatabase.CopyAsset(assetPath, newPath);
            return AssetDatabase.LoadAssetAtPath<T>(newPath);
        }
    }
}
