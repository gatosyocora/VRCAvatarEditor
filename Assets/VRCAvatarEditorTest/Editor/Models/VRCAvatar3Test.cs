using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRCAvatarEditor.Avatars3;

namespace VRCAvatarEditor.Test
{
    public class VRCAvatar3Test
    {
        static List<Object> avatarPrefabs = TestUtility.GetTestAvatars().avatarPrefabs.ToList();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestUtility.OpenTestScene();
        }

        [TestCaseSource("avatarPrefabs")]
        public void LoadAvatarInfoでエラーが発生しない(GameObject avatarPrefab)
        {
            var avatarObject = PrefabUtility.InstantiatePrefab(avatarPrefab) as GameObject;
            var descripter = avatarObject.GetComponent<VRCAvatarDescriptor>();
            var avatar = new VRCAvatar3();
            Assert.DoesNotThrow(() => avatar.LoadAvatarInfo(descripter));
        }

        [TestCaseSource("avatarPrefabs")]
        public void リップシンクが設定できる(GameObject avatarPrefab)
        {
            var avatarObject = PrefabUtility.InstantiatePrefab(avatarPrefab) as GameObject;
            var descripter = TestUtility.ResetComponent<VRCAvatarDescriptor>(avatarObject);
            var avatar = new VRCAvatar3(descripter);
            avatar.SetLipSyncToViseme();
            Assert.IsTrue(descripter.VisemeBlendShapes.All(blendShape => !string.IsNullOrEmpty(blendShape)));
        }
    }
}
