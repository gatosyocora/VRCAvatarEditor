using System.Collections.Generic;
using UnityEngine;
using VRCAvatarEditor.Base;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{
    //[CreateAssetMenu(menuName ="VRCAvatarEditor/Setting Data")]
    public class SettingData : ScriptableObject
    {
        public float defaultZoomDist = 0.5f;
        public float faceZoomDist = 0.1f;
        public float zoomStepDist = 0.1f;

        public bool isGammaCorrection = true;
        public Color monitorBgColor = new Color(0.95f, 0.95f, 0.95f, 1);

        public FaceEmotionViewBase.SortType selectedSortType = FaceEmotionViewBase.SortType.UnSort;
        public List<string> blendshapeExclusions = new List<string> { "vrc.blink_", "vrc.lowerlid_", "mmd" };
        public bool usePreviousAnimationOnHandAnimation = false;

        public VRCAvatarEditorView.LayoutType layoutType = VRCAvatarEditorView.LayoutType.Default;
        public string language = "EN";

        public AvatarMonitorView.MonitorSize monitorSizeType = AvatarMonitorView.MonitorSize.Small;
        public int monitorSize = 0;
    }
}


