using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRCAvatarEditor.Utilities;

namespace VRCAvatarEditor.Test
{
    public class VRCAvatarAnimationUtilityTest
    {
        static List<Object> avatarPrefabs = TestUtility.GetTestAvatars().avatarPrefabs.ToList();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestUtility.OpenTestScene();
        }

        [TestCaseSource("avatarPrefabs")]
        public void GetPlayableLayerでFXLayerが取得できる(GameObject avatarPrefab)
        {
            var avatarObject = PrefabUtility.InstantiatePrefab(avatarPrefab) as GameObject;
            var descripter = avatarObject.GetComponent<VRCAvatarDescriptor>();
            var fxLayer = VRCAvatarAnimationUtility.GetPlayableLayer(descripter, VRCAvatarDescriptor.AnimLayerType.FX);
            Assert.IsNotNull(fxLayer);
        }

        [TestCaseSource("avatarPrefabs")]
        public void GetPlayableLayerでGestureLayerが取得できる(GameObject avatarPrefab)
        {
            var avatarObject = PrefabUtility.InstantiatePrefab(avatarPrefab) as GameObject;
            var descripter = avatarObject.GetComponent<VRCAvatarDescriptor>();
            var gestureLayer = VRCAvatarAnimationUtility.GetPlayableLayer(descripter, VRCAvatarDescriptor.AnimLayerType.Gesture);
            Assert.IsNotNull(gestureLayer);
        }

        [TestCaseSource("avatarPrefabs")]
        public void GetFXLayerIdleStateでIdleStateが取得できる(GameObject avatarPrefab)
        {
            var avatarObject = PrefabUtility.InstantiatePrefab(avatarPrefab) as GameObject;
            var descripter = avatarObject.GetComponent<VRCAvatarDescriptor>();
            var fxController = VRCAvatarAnimationUtility.GetPlayableLayer(descripter, VRCAvatarDescriptor.AnimLayerType.FX).animatorController as AnimatorController;
            var leftIdleState = VRCAvatarAnimationUtility.GetFXLayerIdleState(fxController, VRCAvatarAnimationUtility.HandType.LEFT);
            Assert.IsNotNull(leftIdleState);
            var rightIdleState = VRCAvatarAnimationUtility.GetFXLayerIdleState(fxController, VRCAvatarAnimationUtility.HandType.LEFT);
            Assert.IsNotNull(rightIdleState);
        }
    }
}
