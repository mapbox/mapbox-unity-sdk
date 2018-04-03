// Name this script "EffectRadiusEditor"
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EffectRadius))]
public class EffectRadiusEditor : Editor
{
    public void OnSceneGUI()
    {
        EffectRadius t = (target as EffectRadius);

        EditorGUI.BeginChangeCheck();
        float areaOfEffect = Handles.RadiusHandle(Quaternion.identity, t.transform.position + Vector3.up * 3f, t.areaOfEffect);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Changed Area Of Effect");
            t.areaOfEffect = areaOfEffect;
        }
    }
}
