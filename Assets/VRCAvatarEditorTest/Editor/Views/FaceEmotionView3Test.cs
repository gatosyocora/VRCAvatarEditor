using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRCAvatarEditor.Avatars3;

namespace VRCAvatarEditor.Test
{
    public class FaceEmotionView3Test
    {
        static List<Object> avatarPrefabs = TestUtility.GetTestAvatars().avatarPrefabs.ToList();

        private FaceEmotionView3 view;
        private VRCAvatarEditorView parentWindow;
        private AnimationsView3 animationsGUI;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestUtility.OpenTestScene();
            TestUtility.CreateCacheFolder();
            view = ScriptableObject.CreateInstance<FaceEmotionView3>();
            parentWindow = ScriptableObject.CreateInstance<VRCAvatarEditorView>();
            animationsGUI = ScriptableObject.CreateInstance<AnimationsView3>();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestUtility.DeleteCache();
        }

        [SetUp]
        public void SetUp()
        {
            view.Initialize(null, null, TestUtility.CACHE_FOLDER_PATH, parentWindow, animationsGUI);
        }

        [TestCaseSource("avatarPrefabs")]
        public void OnCreateButtonClicked実行時にエラーが発生しない_CreateAnimationOnly(GameObject avatarPrefab)
        {
            var avatarObject = PrefabUtility.InstantiatePrefab(avatarPrefab) as GameObject;
            var descripter = avatarObject.GetComponent<VRCAvatarDescriptor>();

            var originalAvatar = new VRCAvatar3(descripter);
            originalAvatar.FxController = TestUtility.DuplicateAssetToCache<AnimatorController>(originalAvatar.FxController);
            originalAvatar.AnimSavedFolderPath = TestUtility.CACHE_FOLDER_PATH;

            var editAvatar = new VRCAvatar3();
            
            Assert.DoesNotThrow(() => view.OnCreateButtonClicked(originalAvatar, editAvatar, "testAnimation"));
        }
    }
}
