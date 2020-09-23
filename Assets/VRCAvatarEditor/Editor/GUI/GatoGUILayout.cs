using System;
using UnityEditor;
using UnityEngine;

public static class GatoGUILayout
{
    #region VericalSlider Variable
    private static float sliderPos;
    #endregion

    #region LightRotater Variable
    private static Texture lightActiveTex = Resources.Load<Texture>("Icon/Sun_ON");
    private static Texture lightInactiveTex = Resources.Load<Texture>("Icon/Sun_OFF");
    #endregion

    public static float VerticalSlider(Texture texture, float texSize, float height, float value, float minValue, float maxValue)
    {
        sliderPos = value / (maxValue + minValue);

        var rect = GUILayoutUtility.GetRect(10f, height);

        var boxRect = new Rect(rect.position, new Vector2(7f, height));
        GUI.Box(boxRect, string.Empty);

        var texRect = new Rect(
                            rect.position.x - texSize / 2f + 3f,
                            rect.position.y + (rect.height - sliderPos * height) - texSize / 2f,
                            texSize, texSize
                      );

        var e = Event.current;

        if (texRect.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDrag)
            {
                GUI.changed = true;

                var diff = e.delta.y / height;

                if (sliderPos - diff <= 1 && sliderPos - diff >= 0)
                    sliderPos -= diff;
                else if (sliderPos - diff > 1)
                    sliderPos = 1;
                else if (sliderPos - diff < 0)
                    sliderPos = 0;
            }
        }

        GUI.DrawTexture(texRect, texture);

        value = sliderPos * (maxValue + minValue);

        return value;
    }

    public static float HorizontalSlider(Texture texture, float texSize, float width, float value, float minValue, float maxValue)
    {
        sliderPos = value / (maxValue + minValue);

        var rect = GUILayoutUtility.GetRect(width, 10f);

        Debug.Log(rect.position);
        var boxRect = new Rect(rect.position, new Vector2(width, 7f));
        GUI.Box(boxRect, string.Empty);

        var texRect = new Rect(
                            rect.position.x + (rect.width - sliderPos * width) - texSize / 2f,
                            rect.position.y - texSize / 2f + 3f,
                            texSize, texSize
                      );

        var e = Event.current;

        if (texRect.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDrag)
            {
                GUI.changed = true;

                var diff = e.delta.x / width;

                if (sliderPos - diff <= 1 && sliderPos - diff >= 0)
                    sliderPos -= diff;
                else if (sliderPos - diff > 1)
                    sliderPos = 1;
                else if (sliderPos - diff < 0)
                    sliderPos = 0;
            }
        }

        GUI.DrawTexture(texRect, texture);

        value = sliderPos * (maxValue + minValue);

        return value;
    }

    public static Vector2 MiniMonitor(Texture texture, float width, float height, ref int type, bool isGammaCorrection, Material gammaMat)
    {
        var rect = GUILayoutUtility.GetRect(width, height, GUI.skin.box);

        Graphics.DrawTexture(rect, texture, (isGammaCorrection) ? gammaMat : null);

        var e = Event.current;

        if (rect.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDrag)
            {
                type = (int)e.type;
                return e.delta;
            }
            else if (e.type == EventType.ScrollWheel)
            {
                type = (int)e.type;
                return e.delta;
            }
        }

        return Vector2.zero;
    }

    public static Vector2 LightRotater(float width, float height, ref bool isPressing)
    {
        var rect = GUILayoutUtility.GetRect(width, height, GUI.skin.box);

        GUI.DrawTexture(rect, lightActiveTex, ScaleMode.ScaleToFit, true, 0);

        var e = Event.current;

        if (rect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
        {
            isPressing = true;
        }
        else if (isPressing && e.type == EventType.MouseUp)
        {
            isPressing = false;
        }

        if (e.type == EventType.MouseDrag && isPressing)
        {
            return e.delta;
        }

        return Vector2.zero;
    }

    public static void Button(string text, Action OnPress, bool available = true, params GUILayoutOption[] options)
    {
        using (new EditorGUI.DisabledGroupScope(!available))
        {
            if (GUILayout.Button(text, options))
            {
                OnPress();
            }
        }
    }

    public static T ObjectField<T>(string label, T obj, bool allowSceneObjects = true, params GUILayoutOption[] options) where T : UnityEngine.Object
    {
        return EditorGUILayout.ObjectField(label, obj, typeof(T), allowSceneObjects, options) as T;
    }

    public static void ErrorBox(string errorMessage, bool validate, MessageType type = MessageType.Error)
    {
        if (!validate)
        {
            EditorGUILayout.HelpBox(errorMessage, type);
        }
    }

    public static bool ToggleImage(bool value, Texture onTexture, Texture offTexture, float size = 20f)
    {
        if(GUILayout.Button(value? onTexture : offTexture, GUIStyle.none, GUILayout.Width(size), GUILayout.Height(size)))
        {
            value = !value;
        }
        return value;
    }
}
