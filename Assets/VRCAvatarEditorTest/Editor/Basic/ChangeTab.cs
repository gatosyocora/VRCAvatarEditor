using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRCSDK2;

namespace VRCAvatarEditor.Test.Basic
{
    public class ChangeTab
    {
        TestAvatars testAvatars;
        VRCAvatarEditorGUI editorGUI;

        public ChangeTab()
        {
            testAvatars = Resources.Load<TestAvatars>("TestAvatars");
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            editorGUI = ScriptableObject.CreateInstance<VRCAvatarEditorGUI>();
        }

        [Test]
        public void HaveNoErrorIfChangeFaceEmotionTab()
        {
            foreach (Object avatarPrefab in testAvatars.testAvatars)
            {
                Debug.Log($"[{avatarPrefab.name}]");
                var avatar = PrefabUtility.InstantiatePrefab(avatarPrefab) as GameObject;
                editorGUI.TargetAvatarDescriptor = avatar.GetComponent<VRC_AvatarDescriptor>();
                editorGUI.CurrentTool = VRCAvatarEditorGUI.ToolFunc.FaceEmotion;
            }
        }

        [Test]
        public void HaveNoErrorIfChangeProbeAnchorTab()
        {
            foreach (Object avatarPrefab in testAvatars.testAvatars)
            {
                Debug.Log($"[{avatarPrefab.name}]");
                var avatar = PrefabUtility.InstantiatePrefab(avatarPrefab) as GameObject;
                editorGUI.TargetAvatarDescriptor = avatar.GetComponent<VRC_AvatarDescriptor>();
                editorGUI.CurrentTool = VRCAvatarEditorGUI.ToolFunc.ProbeAnchor;
            }
        }

        [Test]
        public void HaveNoErrorIfChangeBoundsTab()
        {
            foreach (Object avatarPrefab in testAvatars.testAvatars)
            {
                Debug.Log($"[{avatarPrefab.name}]");
                var avatar = PrefabUtility.InstantiatePrefab(avatarPrefab) as GameObject;
                editorGUI.TargetAvatarDescriptor = avatar.GetComponent<VRC_AvatarDescriptor>();
                editorGUI.CurrentTool = VRCAvatarEditorGUI.ToolFunc.Bounds;
            }
        }

        [Test]
        public void HaveNoErrorIfChangeShaderTab()
        {
            foreach (Object avatarPrefab in testAvatars.testAvatars)
            {
                Debug.Log($"[{avatarPrefab.name}]");
                var avatar = PrefabUtility.InstantiatePrefab(avatarPrefab) as GameObject;
                editorGUI.TargetAvatarDescriptor = avatar.GetComponent<VRC_AvatarDescriptor>();
                editorGUI.CurrentTool = VRCAvatarEditorGUI.ToolFunc.Shader;
            }
        }

        [Test]
        public void HaveNoErrorIfChangeAvatarInfoTab()
        {
            foreach (Object avatarPrefab in testAvatars.testAvatars)
            {
                Debug.Log($"[{avatarPrefab.name}]");
                var avatar = PrefabUtility.InstantiatePrefab(avatarPrefab) as GameObject;
                editorGUI.TargetAvatarDescriptor = avatar.GetComponent<VRC_AvatarDescriptor>();
                editorGUI.CurrentTool = VRCAvatarEditorGUI.ToolFunc.AvatarInfo;
            }
        }
    }
}