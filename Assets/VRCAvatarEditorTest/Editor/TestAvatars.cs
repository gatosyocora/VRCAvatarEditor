using UnityEngine;

namespace VRCAvatarEditor.Test
{
    [CreateAssetMenu(menuName = "VRCAvatarEditor/Test/Avatars")]
    public class TestAvatars : ScriptableObject
    {
        [SerializeField]
        private Object[] testAvatars;
    }
}