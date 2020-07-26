using UnityEditor;
using UnityEngine;

namespace VRCAvatarEditor
{
    public class ShaderGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatarEditor.Avatar avatar;

        private Vector2 leftScrollPosShader = Vector2.zero;

        public void Initialize(ref VRCAvatarEditor.Avatar avatar)
        {
            this.avatar = avatar;
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            EditorGUILayout.LabelField(LocalizeText.instance.langPair.shaderTitle, EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(leftScrollPosShader))
                {
                    leftScrollPosShader = scrollView.scrollPosition;
                    if (avatar.materials != null)
                    {
                        foreach (var mat in avatar.materials)
                        {
                            if (mat == null) continue;
                            if (mat.shader == null) continue;

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("" + mat.name + ".mat", GUILayout.Width(200f));
                                EditorGUILayout.LabelField(mat.shader.name);
                                if (GUILayout.Button(LocalizeText.instance.langPair.select))
                                {
                                    Selection.activeObject = mat;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public void DrawSettingsGUI() { }
        public void LoadSettingData(SettingData settingAsset) { }
        public void SaveSettingData(ref SettingData settingAsset) { }
        public void Dispose() { }
    }
}
