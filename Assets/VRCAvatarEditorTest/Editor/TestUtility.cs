using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRCAvatarEditor.Test
{
    public static class TestUtility
    {
        public static TestAvatars GetTestAvatars()
        {
            return AssetDatabase.LoadAssetAtPath<TestAvatars>(
                "Assets/VRCAvatarEditorTest/Editor/TestAvatars.asset"
            );
        }
    }
}
