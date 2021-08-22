using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRCAvatarEditor.Avatars3;
using VRCAvatarEditor.Base;
using VRCAvatarEditor.Interfaces;

namespace VRCAvatarEditor.Test
{
    public class FaceEmotionView3Test
    {
        static List<Object> avatarPrefabs = TestUtility.GetTestAvatars().avatarPrefabs.ToList();

        private FaceEmotionView3 view;
        private VRCAvatarEditorView parentWindow;
        private AnimationsView3 animationsGUI;

        private IFaceEmotion mockFaceEmotion = new MockFaceEmotion();

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
            view.Initialize(mockFaceEmotion, null, null, TestUtility.CACHE_FOLDER_PATH, parentWindow, animationsGUI);
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

        public class MockFaceEmotion : IFaceEmotion
        {
            void IFaceEmotion.ApplyAnimationProperties(List<AnimParam> animProperties, VRCAvatarBase avatar)
            {
                throw new System.NotImplementedException();
            }

            void IFaceEmotion.ApplyAnimationProperties(AnimationClip clip, VRCAvatarBase avatar)
            {
                throw new System.NotImplementedException();
            }

            void IFaceEmotion.CopyAnimationKeysFromOriginClip(AnimationClip originClip, AnimationClip targetClip)
            {
                throw new System.NotImplementedException();
            }

            AnimationClip IFaceEmotion.CreateBlendShapeAnimationClip(string fileName, string saveFolderPath, VRCAvatarBase avatar)
            {
                return TestUtility.LoadAssetFromName<AnimationClip>("DummyAnimation.anim");
            }

            List<AnimParam> IFaceEmotion.GetAnimationParamaters(AnimationClip clip)
            {
                throw new System.NotImplementedException();
            }

            List<AnimParam> IFaceEmotion.GetAvatarFaceParamaters(List<SkinnedMesh> skinnedMeshList)
            {
                throw new System.NotImplementedException();
            }

            void IFaceEmotion.LoadAnimationProperties(FaceEmotionViewBase faceEmotionGUI, VRCAvatarEditorView editorGUI)
            {
                throw new System.NotImplementedException();
            }

            void IFaceEmotion.ResetAllBlendShapeValues(VRCAvatarBase avatar)
            {
                throw new System.NotImplementedException();
            }

            void IFaceEmotion.ResetToDefaultFaceEmotion(VRCAvatarBase avatar)
            {
            }

            bool IFaceEmotion.SetBlendShapeMaxValue(SkinnedMeshRenderer renderer, int id)
            {
                throw new System.NotImplementedException();
            }

            bool IFaceEmotion.SetBlendShapeMinValue(SkinnedMeshRenderer renderer, int id)
            {
                throw new System.NotImplementedException();
            }

            bool IFaceEmotion.SetContainsAll(bool value, List<SkinnedMesh.BlendShape> blendshapes)
            {
                throw new System.NotImplementedException();
            }

            void IFaceEmotion.SetToDefaultFaceEmotion(VRCAvatarBase editAvatar, VRCAvatarBase originalAvatar)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
