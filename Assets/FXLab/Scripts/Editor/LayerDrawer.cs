using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LayerAttribute))]
public class LayerDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
    {
        var value = prop.intValue;
        HandleLayer("Culling Mask", position, ref value);
        if (value != prop.intValue)
            prop.intValue = value;
    }

    public static void HandleLayer(string label, Rect? position, ref int layer)
    {
        var layerMasksOptions = Enumerable.Range(0, 31).Select(i => LayerMask.LayerToName(i)).Where(m => !string.IsNullOrEmpty(m)).ToArray();
        var currentMask = 0;
        for (var i = 0; i < layerMasksOptions.Length; ++i)
        {
            if ((layer & (1 << LayerMask.NameToLayer(layerMasksOptions[i]))) != 0)
                currentMask |= 1 << i;
        }

        int newMask;
        if (string.IsNullOrEmpty(label))
        {
            if (position.HasValue)
                newMask = EditorGUI.MaskField(position.Value, currentMask, layerMasksOptions);
            else
                newMask = EditorGUILayout.MaskField(currentMask, layerMasksOptions);
        }
        else
        {
            if (position.HasValue)
                newMask = EditorGUI.MaskField(position.Value, label, currentMask, layerMasksOptions);
            else
                newMask = EditorGUILayout.MaskField(label, currentMask, layerMasksOptions);
        }
        if (newMask != currentMask)
        {
            var finalMask = 0;
            if (newMask != -1)
            {
                for (var i = 0; i < layerMasksOptions.Length; ++i)
                {
                    if ((newMask & (1 << i)) != 0)
                        finalMask |= 1 << LayerMask.NameToLayer(layerMasksOptions[i]);
                }
            }
            else
                finalMask = -1;
            layer = finalMask;
        }
    }

   
}