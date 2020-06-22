using UnityEngine;
using UnityEditor;
using VRCAvatarEditor;

namespace VRCAvatarEditor
{
    public class ProbeAnchorGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatarEditor.Avatar avatar;

        private ProbeAnchor.TARGETPOS targetPos = ProbeAnchor.TARGETPOS.HEAD;

        private bool isGettingSkinnedMeshRenderer = true;
        private bool isGettingMeshRenderer = true;

        private bool isOpeningRendererList = false;

        private bool[] isSettingToSkinnedMesh = null;
        private bool[] isSettingToMesh = null;

        private Vector2 leftScrollPos = Vector2.zero;

        public void Initialize(ref VRCAvatarEditor.Avatar avatar)
        {
            this.avatar = avatar;
            SettingForProbeSetter();
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            EditorGUILayout.LabelField("Probe Anchor", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                // 設定するRendererの選択
                isGettingSkinnedMeshRenderer = EditorGUILayout.Toggle("Set To SkinnedMeshRenderer", isGettingSkinnedMeshRenderer);
                isGettingMeshRenderer = EditorGUILayout.Toggle("Set To MeshRenderer", isGettingMeshRenderer);

                // ライティングの計算の基準とする位置を選択
                targetPos = (ProbeAnchor.TARGETPOS)EditorGUILayout.EnumPopup("TargetPosition", targetPos);

                // Rendererの一覧を表示
                if (avatar != null && avatar.descriptor != null)
                {
                    isOpeningRendererList = EditorGUILayout.Foldout(isOpeningRendererList, "Renderer List");

                    if (isOpeningRendererList)
                    {
                        using (var scrollView = new EditorGUILayout.ScrollViewScope(leftScrollPos))
                        {
                            leftScrollPos = scrollView.scrollPosition;

                            using (new EditorGUI.IndentLevelScope())
                            {
                                int index = 0;

                                if (isGettingSkinnedMeshRenderer && avatar.skinnedMeshRendererList != null && isSettingToSkinnedMesh != null)
                                {
                                    foreach (var skinnedMesh in avatar.skinnedMeshRendererList)
                                    {
                                        if (skinnedMesh == null) continue;

                                        using (new GUILayout.HorizontalScope())
                                        {
                                            isSettingToSkinnedMesh[index] = EditorGUILayout.Toggle(skinnedMesh.gameObject.name, isSettingToSkinnedMesh[index]);
                                            if (GUILayout.Button("Select"))
                                                Selection.activeGameObject = skinnedMesh.gameObject;
                                        }

                                        index++;
                                    }
                                }

                                index = 0;

                                if (isGettingMeshRenderer && avatar.meshRendererList != null && isSettingToMesh != null)
                                {
                                    foreach (var mesh in avatar.meshRendererList)
                                    {
                                        if (mesh == null) continue;

                                        using (new GUILayout.HorizontalScope())
                                        {
                                            isSettingToMesh[index] = EditorGUILayout.Toggle(mesh.gameObject.name, isSettingToMesh[index]);
                                            if (GUILayout.Button("Select"))
                                                Selection.activeGameObject = mesh.gameObject;
                                        }

                                        index++;

                                    }
                                }
                            }

                            EditorGUILayout.HelpBox("チェックがついているメッシュのProbeAnchorが設定されます", MessageType.Info);
                        }
                    }
                }
            }

            if (GUILayout.Button("Set ProbeAnchor"))
            {
                GameObject anchorTarget = null;
                var result = ProbeAnchor.CreateAndSetProbeAnchorObject(avatar.descriptor.gameObject, targetPos, ref anchorTarget);
                if (result && isGettingSkinnedMeshRenderer)
                    ProbeAnchor.SetProbeAnchorToSkinnedMeshRenderers(ref anchorTarget, ref avatar, ref isSettingToSkinnedMesh);
                if (result && isGettingMeshRenderer)
                    ProbeAnchor.SetProbeAnchorToMeshRenderers(ref anchorTarget, ref avatar, ref isSettingToMesh);
            }

            return false;
        }
        public void DrawSettingsGUI() { }
        public void LoadSettingData(SettingData settingAsset) { }
        public void SaveSettingData(ref SettingData settingAsset) { }
        public void Dispose() { }

        public void SettingForProbeSetter()
        {
            if (avatar == null || avatar.skinnedMeshRendererList == null || avatar.meshRendererList == null)
                return;

            isSettingToSkinnedMesh = new bool[avatar.skinnedMeshRendererList.Count];
            for (int i = 0; i < avatar.skinnedMeshRendererList.Count; i++) isSettingToSkinnedMesh[i] = true;
            isSettingToMesh = new bool[avatar.meshRendererList.Count];
            for (int i = 0; i < avatar.meshRendererList.Count; i++) isSettingToMesh[i] = true;
        }
    }
}
