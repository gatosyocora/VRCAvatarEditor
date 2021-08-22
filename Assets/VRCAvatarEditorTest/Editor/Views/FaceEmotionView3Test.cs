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
    }
}
