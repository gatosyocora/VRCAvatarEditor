using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRCAvatarEditor;
using System.Linq;

namespace VRCAvatarEditor
{
    public class MeshBoundsGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatarEditor.Avatar avatar;

        public List<SkinnedMeshRenderer> targetRenderers;
        private List<SkinnedMeshRenderer> exclusions = new List<SkinnedMeshRenderer>();

        public MeshBoundsGUI(ref VRCAvatarEditor.Avatar avatar)
        {
            this.avatar = avatar;
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {

            // TODO: UIの見直し
            if (targetRenderers == null && avatar != null)
            {
                targetRenderers = MeshBounds.GetSkinnedMeshRenderersWithoutExclusions(
                                    avatar.descriptor.gameObject,
                                    exclusions);
            }

            EditorGUILayout.LabelField("Bounds", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Reset Bounds to Prefab"))
                {
                    MeshBounds.RevertBoundsToPrefab(targetRenderers);
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Exclusions");

                using (new EditorGUI.IndentLevelScope())
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        var parentObject = EditorGUILayout.ObjectField(
                            "Child objects",
                            null,
                            typeof(GameObject),
                            true
                        ) as GameObject;

                        if (check.changed && parentObject != null && avatar != null)
                        {
                            var renderers = parentObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                            foreach (var renderer in renderers)
                            {
                                exclusions.Add(renderer);
                            }
                            exclusions = exclusions.Distinct().ToList();

                            targetRenderers = MeshBounds.GetSkinnedMeshRenderersWithoutExclusions(
                                                            avatar.descriptor.gameObject,
                                                            exclusions);
                        }
                    }

                    EditorGUILayout.Space();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("+", GUILayout.MaxWidth(60)))
                        {
                            exclusions.Add(null);
                        }
                    }

                    using (var check = new EditorGUI.ChangeCheckScope())
                    {

                        for (int i = 0; i < exclusions.Count; i++)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                exclusions[i] = EditorGUILayout.ObjectField(
                                    "Object " + (i + 1),
                                    exclusions[i],
                                    typeof(SkinnedMeshRenderer),
                                    true
                                ) as SkinnedMeshRenderer;

                                if (GUILayout.Button("x", GUILayout.MaxWidth(30)))
                                {
                                    exclusions.RemoveAt(i);
                                }
                            }
                        }

                        if (check.changed && avatar != null)
                        {
                            targetRenderers = MeshBounds.GetSkinnedMeshRenderersWithoutExclusions(
                                                avatar.descriptor.gameObject,
                                                exclusions);
                        }
                    }

                    EditorGUILayout.Space();
                }
            }

            if (GUILayout.Button("Set Bounds"))
            {
                MeshBounds.BoundsSetter(targetRenderers);
            }
            return false;
        }

        public void DrawSettingsGUI() { }
        public void LoadSettingData(SettingData settingAsset) { }
        public void SaveSettingData(ref SettingData settingAsset) { }
        public void Dispose() { }
    }
}
