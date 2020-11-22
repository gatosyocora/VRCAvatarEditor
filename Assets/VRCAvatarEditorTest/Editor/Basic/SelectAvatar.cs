using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRCSDK2;

namespace VRCAvatarEditor.Test.Basic
{
    public class SelectAvatar
    {
        [Test]
        public void HaveNoError()
        {
            var testAvatars = Resources.Load<TestAvatars>("TestAvatars");
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var editorGUI = ScriptableObject.CreateInstance<VRCAvatarEditorGUI>();
            
            foreach (Object avatarPrefab in testAvatars.testAvatars)
            {
                Debug.Log($"[{avatarPrefab.name}]");
                var avatar = PrefabUtility.InstantiatePrefab(avatarPrefab) as GameObject;
                editorGUI.TargetAvatarDescriptor = avatar.GetComponent<VRC_AvatarDescriptor>();
            }
        }
    }
}