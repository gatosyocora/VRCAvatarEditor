using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using VRCAvatarEditor;

namespace VRCAvatarEditor {
	public class AnimationLoaderGUI : EditorWindow {

		private SendData sendData;

		private string filePath;
		private string fileName;

		public class AnimParam {
			public string objPath;
			public string blendShapeName;
			public float value;
			public bool isSelect;

			public AnimParam(string path, string propertyName, float value){
				objPath = path;
				this.blendShapeName = propertyName.Replace("blendShape.", "");
				this.value = value;
				isSelect = true;
			}
		}

		private List<AnimParam> animParamList;

		private Vector2 scrollPos = Vector2.zero;

		void OnEnable() {
			    sendData = AssetDatabase.LoadAssetAtPath<SendData>(FaceEmotion.SENDDATAASSET_PATH);
				filePath = sendData.filePath;

				fileName = Path.GetFileName(filePath);

				var anim = AssetDatabase.LoadAssetAtPath(filePath, typeof(AnimationClip)) as AnimationClip;
				var bindings = AnimationUtility.GetCurveBindings(anim);

				animParamList = new List<AnimParam>();

				foreach (var binding in bindings) {
					
					if ((binding.propertyName).Split('.')[0] != "blendShape") continue;

					var curve = AnimationUtility.GetEditorCurve(anim, binding);
					
					var animParam = new AnimParam(binding.path, binding.propertyName, curve[0].value);

					animParamList.Add(animParam);
				}
		}

		void OnGUI() {

			EditorGUILayout.LabelField("Animation Name", fileName);

			EditorGUILayout.LabelField("Properties");
			using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPos, GUI.skin.box)) {
				scrollPos = scroll.scrollPosition;
				for (int i = 0; i < animParamList.Count; i++) {
					using (new EditorGUILayout.HorizontalScope()) {
						animParamList[i].isSelect = EditorGUILayout.ToggleLeft(animParamList[i].blendShapeName, animParamList[i].isSelect);
						GUILayout.Label(animParamList[i].value+"");
					}
				}
			}

			if (GUILayout.Button("Load Properties")) {
				LoadAnimationProperties();
			}
		}

		private void LoadAnimationProperties() {
			sendData.loadingProperties = animParamList.Where(x => x.isSelect).ToList();
			sendData.window.SendEvent(EditorGUIUtility.CommandEvent("ApplyAnimationProperties"));
			this.Close();
		}
	}
}

