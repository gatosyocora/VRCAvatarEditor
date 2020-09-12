using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRCAvatar = VRCAvatarEditor.Avatars2.VRCAvatar2;

namespace VRCAvatarEditor
{
    public class MeshBoundsGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatar avatar;

        public List<SkinnedMeshRenderer> targetRenderers;
        private List<SkinnedMeshRenderer> exclusions;

        public void Initialize(VRCAvatar avatar)
        {
            this.avatar = avatar;
            exclusions = new List<SkinnedMeshRenderer>();
            targetRenderers = MeshBounds.GetSkinnedMeshRenderersWithoutExclusions(
                                    avatar.Descriptor.gameObject,
                                    exclusions);
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            EditorGUILayout.LabelField(LocalizeText.instance.langPair.boundsTitle, EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    GatoGUILayout.Button(
                        LocalizeText.instance.langPair.resetToBoundsToPrefabButtonText,
                        () => {
                            MeshBounds.RevertBoundsToPrefab(targetRenderers);
                        });
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField(LocalizeText.instance.langPair.exclusions);

                using (new EditorGUI.IndentLevelScope())
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        var parentObject = GatoGUILayout.ObjectField<GameObject>(
                                                LocalizeText.instance.langPair.childObjectsLabel,
                                                null);

                        if (check.changed && parentObject != null && avatar != null)
                        {
                            var renderers = parentObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                            foreach (var renderer in renderers)
                            {
                                exclusions.Add(renderer);
                            }
                            exclusions = exclusions.Distinct().ToList();

                            targetRenderers = MeshBounds.GetSkinnedMeshRenderersWithoutExclusions(
                                                            avatar.Descriptor.gameObject,
                                                            exclusions);
                        }
                    }

                    EditorGUILayout.Space();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        GatoGUILayout.Button(
                            "+",
                            () => {
                                exclusions.Add(null);
                            },
                            true,
                            GUILayout.MaxWidth(60));
                    }

                    using (var check = new EditorGUI.ChangeCheckScope())
                    {

                        for (int i = 0; i < exclusions.Count; i++)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                exclusions[i] = GatoGUILayout.ObjectField(
                                                    "Object " + (i + 1),
                                                    exclusions[i]);

                                GatoGUILayout.Button(
                                    "x",
                                    () => {
                                        exclusions.RemoveAt(i);
                                    },
                                    true,
                                    GUILayout.MaxWidth(30));
                            }
                        }

                        if (check.changed && avatar != null)
                        {
                            targetRenderers = MeshBounds.GetSkinnedMeshRenderersWithoutExclusions(
                                                avatar.Descriptor.gameObject,
                                                exclusions);
                        }
                    }

                    EditorGUILayout.Space();
                }
            }

            GatoGUILayout.Button(
                LocalizeText.instance.langPair.setBoundsButtonText,
                () => {
                    MeshBounds.BoundsSetter(targetRenderers);
                });

            return false;
        }

        public void DrawSettingsGUI() { }
        public void LoadSettingData(SettingData settingAsset) { }
        public void SaveSettingData(ref SettingData settingAsset) { }
        public void Dispose() { }

        /// <summary>
        /// BoundsサイズをSceneViewに表示する
        /// </summary>
        /// <param name="renderer"></param>
        public void DrawBoundsGizmo()
        {
            if (targetRenderers == null) return;

            foreach (var renderer in targetRenderers)
            {
                var bounds = renderer.bounds;
                Handles.color = Color.white;
                Handles.DrawWireCube(bounds.center, bounds.size);
            }
        }
    }
}
