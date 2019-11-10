using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using VRCAvatarEditor;

namespace VRCAvatarEditor
{
    public class FaceEmotionGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatarEditor.Avatar avatar;
        private VRCAvatarEditorGUI parentWindow;

        private static readonly string DEFAULT_ANIM_NAME = "faceAnim";
        private HandPose.HandPoseType selectedHandAnim = HandPose.HandPoseType.None;

        private Vector2 scrollPos = Vector2.zero;

        private bool isExclusionKey;

        public enum SortType
        {
            UnSort,
            AToZ,
        }

        public SortType selectedSortType = SortType.UnSort;
        public List<string> blendshapeExclusions = new List<string> { "vrc.v_", "vrc.blink_", "vrc.lowerlid_", "vrc.owerlid_", "mmd" };

        private bool isOpeningBlendShapeExclusionList = false;

        private SendData sendData;

        private string saveFolderPath;
        private string animName;

        public FaceEmotionGUI(ref VRCAvatarEditor.Avatar avatar, string saveFolderPath, EditorWindow window)
        {
            this.avatar = avatar;
            animName = DEFAULT_ANIM_NAME;
            this.saveFolderPath = saveFolderPath;
            this.parentWindow = window as VRCAvatarEditorGUI;
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            if (Event.current.type == EventType.ExecuteCommand &&
                Event.current.commandName == "ApplyAnimationProperties")
            {
                FaceEmotion.ApplyAnimationProperties(sendData.loadingProperties, ref avatar);
            }

            EditorGUILayout.LabelField("表情設定", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUI.DisabledScope(avatar.descriptor == null))
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Load Animation"))
                    {
                        sendData = CreateInstance<SendData>();
                        var result = FaceEmotion.LoadAnimationProperties(ref sendData, parentWindow);

                        if (result)
                        {
                            parentWindow.OpenSubWindow();
                        }
                    }
                }

                if (avatar.skinnedMeshList != null)
                {
                    BlendShapeListGUI();
                }

                animName = EditorGUILayout.TextField("AnimClipFileName", animName);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("AnimClipSaveFolder", saveFolderPath);

                    if (GUILayout.Button("Select Folder", GUILayout.Width(100)))
                    {
                        saveFolderPath = EditorUtility.OpenFolderPanel("Select saved folder", saveFolderPath, string.Empty);
                        saveFolderPath = FileUtil.GetProjectRelativePath(saveFolderPath);
                        if (saveFolderPath == "/") saveFolderPath = "Assets/";
                        parentWindow.animationsGUI.UpdateSaveFolderPath(saveFolderPath);
                    }

                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    selectedHandAnim = (HandPose.HandPoseType)EditorGUILayout.EnumPopup("HandPose", selectedHandAnim);
                    using (new EditorGUI.DisabledGroupScope(selectedHandAnim == HandPose.HandPoseType.None))
                    {
                        if (GUILayout.Button("Create AnimFile"))
                        {
                            var animController = avatar.standingAnimController;

                            var createdAnimClip = FaceEmotion.CreateBlendShapeAnimationClip(animName, saveFolderPath, ref avatar, ref blendshapeExclusions, avatar.descriptor.gameObject);
                            if (selectedHandAnim != HandPose.HandPoseType.None)
                            {
                                HandPose.AddHandPoseAnimationKeysFromOriginClip(ref createdAnimClip, selectedHandAnim);
                                animController[AnimationsGUI.HANDANIMS[(int)selectedHandAnim - 1]] = createdAnimClip;
                            }

                            avatar.standingAnimController = animController;
                        }
                    }
                    if (GUILayout.Button("Reset All"))
                    {
                        FaceEmotion.ResetAllBlendShapeValues(ref avatar);
                    }
                }

                EditorGUILayout.HelpBox("Reset Allを押すとチェックをいれているすべてのシェイプキーの値が最低値になります", MessageType.Warning);

            }

            return false;
        }

        public void DrawSettingsGUI()
        {
            EditorGUILayout.LabelField("FaceEmotion Creator", EditorStyles.boldLabel);

            selectedSortType = (SortType)EditorGUILayout.EnumPopup("SortType", selectedSortType);

            isOpeningBlendShapeExclusionList = EditorGUILayout.Foldout(isOpeningBlendShapeExclusionList, "Blendshape Exclusions");
            if (isOpeningBlendShapeExclusionList)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    for (int i = 0; i < blendshapeExclusions.Count; i++)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            blendshapeExclusions[i] = EditorGUILayout.TextField(blendshapeExclusions[i]);
                            if (GUILayout.Button("Remove"))
                                blendshapeExclusions.RemoveAt(i);
                        }
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add"))
                        blendshapeExclusions.Add(string.Empty);
                }
            }
        }

        public void LoadSettingData(SettingData settingAsset)
        {
            selectedSortType = settingAsset.selectedSortType;
            blendshapeExclusions = new List<string>(settingAsset.blendshapeExclusions);
        }

        public void SaveSettingData(ref SettingData settingAsset)
        {
            settingAsset.selectedSortType = selectedSortType;
            settingAsset.blendshapeExclusions = new List<string>(blendshapeExclusions);
        }

        public void Dispose() { }

        private void BlendShapeListGUI()
        {
            // BlendShapeのリスト
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos))
            {
                scrollPos = scrollView.scrollPosition;
                foreach (var skinnedMesh in avatar.skinnedMeshList)
                {
                    skinnedMesh.isOpenBlendShapes = EditorGUILayout.Foldout(skinnedMesh.isOpenBlendShapes, skinnedMesh.objName);
                    if (skinnedMesh.isOpenBlendShapes)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                using (var check = new EditorGUI.ChangeCheckScope())
                                {
                                    skinnedMesh.isContainsAll = EditorGUILayout.ToggleLeft(string.Empty, skinnedMesh.isContainsAll, GUILayout.Width(45));
                                    if (check.changed)
                                    {
                                        FaceEmotion.SetContainsAll(skinnedMesh.isContainsAll, ref skinnedMesh.blendshapes);
                                    }
                                }
                                EditorGUILayout.LabelField("Toggle All", GUILayout.Height(20));
                            }

                            foreach (var blendshape in skinnedMesh.blendshapes)
                            {
                                if (!blendshape.isExclusion)
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        blendshape.isContains = EditorGUILayout.ToggleLeft(string.Empty, blendshape.isContains, GUILayout.Width(45));

                                        EditorGUILayout.SelectableLabel(blendshape.name, GUILayout.Height(20));
                                        using (var check = new EditorGUI.ChangeCheckScope())
                                        {
                                            var value = skinnedMesh.renderer.GetBlendShapeWeight(blendshape.id);
                                            value = EditorGUILayout.Slider(value, 0, 100);
                                            if (check.changed)
                                                skinnedMesh.renderer.SetBlendShapeWeight(blendshape.id, value);
                                        }

                                        if (GUILayout.Button("Min", GUILayout.MaxWidth(50)))
                                        {
                                            FaceEmotion.SetBlendShapeMinValue(ref skinnedMesh.renderer, blendshape.id);
                                        }
                                        if (GUILayout.Button("Max", GUILayout.MaxWidth(50)))
                                        {
                                            FaceEmotion.SetBlendShapeMaxValue(ref skinnedMesh.renderer, blendshape.id);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
