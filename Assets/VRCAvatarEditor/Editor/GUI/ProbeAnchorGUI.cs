using UnityEditor;
using UnityEngine;
using Avatar = VRCAvatarEditor.VRCAvatar;

namespace VRCAvatarEditor
{
    public class ProbeAnchorGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatar avatar;

        private ProbeAnchor.TARGETPOS targetPos = ProbeAnchor.TARGETPOS.HEAD;

        private bool isGettingSkinnedMeshRenderer = true;
        private bool isGettingMeshRenderer = true;

        private bool isOpeningRendererList = false;

        private bool[] isSettingToSkinnedMesh = null;
        private bool[] isSettingToMesh = null;

        private Vector2 leftScrollPos = Vector2.zero;

        public void Initialize(VRCAvatar avatar)
        {
            this.avatar = avatar;
            SettingForProbeSetter();
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            EditorGUILayout.LabelField(LocalizeText.instance.langPair.probeAnchorTitle, EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                // 設定するRendererの選択
                isGettingSkinnedMeshRenderer = EditorGUILayout.Toggle(LocalizeText.instance.langPair.setToSkinnedMeshRendererLabel, isGettingSkinnedMeshRenderer);
                isGettingMeshRenderer = EditorGUILayout.Toggle(LocalizeText.instance.langPair.setToMeshRendererLabel, isGettingMeshRenderer);

                // ライティングの計算の基準とする位置を選択
                targetPos = (ProbeAnchor.TARGETPOS)EditorGUILayout.EnumPopup(LocalizeText.instance.langPair.targetPositionLabel, targetPos);

                // Rendererの一覧を表示
                if (avatar != null && avatar.Descriptor != null)
                {
                    isOpeningRendererList = EditorGUILayout.Foldout(isOpeningRendererList, LocalizeText.instance.langPair.rendererListLabel);

                    if (isOpeningRendererList)
                    {
                        using (var scrollView = new EditorGUILayout.ScrollViewScope(leftScrollPos))
                        {
                            leftScrollPos = scrollView.scrollPosition;

                            using (new EditorGUI.IndentLevelScope())
                            {
                                int index = 0;

                                if (isGettingSkinnedMeshRenderer && avatar.SkinnedMeshRendererList != null && isSettingToSkinnedMesh != null)
                                {
                                    foreach (var skinnedMesh in avatar.SkinnedMeshRendererList)
                                    {
                                        if (skinnedMesh == null) continue;

                                        using (new GUILayout.HorizontalScope())
                                        {
                                            isSettingToSkinnedMesh[index] = EditorGUILayout.Toggle(skinnedMesh.gameObject.name, isSettingToSkinnedMesh[index]);
                                            GatoGUILayout.Button(LocalizeText.instance.langPair.select,
                                                () => {
                                                    Selection.activeGameObject = skinnedMesh.gameObject;
                                                });
                                        }

                                        index++;
                                    }
                                }

                                index = 0;

                                if (isGettingMeshRenderer && avatar.MeshRendererList != null && isSettingToMesh != null)
                                {
                                    foreach (var mesh in avatar.MeshRendererList)
                                    {
                                        if (mesh == null) continue;

                                        using (new GUILayout.HorizontalScope())
                                        {
                                            isSettingToMesh[index] = EditorGUILayout.Toggle(mesh.gameObject.name, isSettingToMesh[index]);
                                            GatoGUILayout.Button(LocalizeText.instance.langPair.select,
                                                () => {
                                                    Selection.activeGameObject = mesh.gameObject;
                                                });
                                        }

                                        index++;

                                    }
                                }
                            }

                            EditorGUILayout.HelpBox(LocalizeText.instance.langPair.probeAnchorMessageText, MessageType.Info);
                        }
                    }
                }
            }

            GatoGUILayout.Button(LocalizeText.instance.langPair.setProbeAnchorButtonText,
                () => {
                    GameObject anchorTarget = null;
                    var result = ProbeAnchor.CreateAndSetProbeAnchorObject(avatar.Descriptor.gameObject, targetPos, ref anchorTarget);
                    if (result && isGettingSkinnedMeshRenderer)
                        ProbeAnchor.SetProbeAnchorToSkinnedMeshRenderers(ref anchorTarget, ref avatar, ref isSettingToSkinnedMesh);
                    if (result && isGettingMeshRenderer)
                        ProbeAnchor.SetProbeAnchorToMeshRenderers(ref anchorTarget, ref avatar, ref isSettingToMesh);
                });

            return false;
        }
        public void DrawSettingsGUI() { }
        public void LoadSettingData(SettingData settingAsset) { }
        public void SaveSettingData(ref SettingData settingAsset) { }
        public void Dispose() { }

        public void SettingForProbeSetter()
        {
            if (avatar == null || avatar.SkinnedMeshRendererList == null || avatar.MeshRendererList == null)
                return;

            isSettingToSkinnedMesh = new bool[avatar.SkinnedMeshRendererList.Count];
            for (int i = 0; i < avatar.SkinnedMeshRendererList.Count; i++) isSettingToSkinnedMesh[i] = true;
            isSettingToMesh = new bool[avatar.MeshRendererList.Count];
            for (int i = 0; i < avatar.MeshRendererList.Count; i++) isSettingToMesh[i] = true;
        }
    }
}
