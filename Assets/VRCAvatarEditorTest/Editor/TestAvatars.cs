using UnityEngine;

namespace VRCAvatarEditor.Test
{
    [CreateAssetMenu(menuName = "VRCAvatarEditor/Test/Avatars")]
    public class TestAvatars : ScriptableObject
    {
        [SerializeField]
        public Object[] testAvatars;
    }
}