using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Lean.Touch
{
	[CustomEditor(typeof(LeanTouch))]
	public class LeanTouch_Editor : Editor
	{
		private static List<LeanFinger> allFingers = new List<LeanFinger>();
		
		private static GUIStyle fadingLabel;
		
		public static GUIStyle GetFadingLabel(bool active, float progress)
		{
			if (fadingLabel == null)
			{
				fadingLabel = new GUIStyle(EditorStyles.label);
			}
			
			var a = EditorStyles.label.normal.textColor;
			var b = a; b.a = active == true ? 0.5f : 0.0f;
			
			fadingLabel.normal.textColor = Color.Lerp(a, b, progress);
			
			return fadingLabel;
		}
		
		[MenuItem("GameObject/Lean/Touch", false, 1)]
		public static void CreateTouch()
		{
			var gameObject = new GameObject(typeof(LeanTouch).Name);
			
			UnityEditor.Undo.RegisterCreatedObjectUndo(gameObject, "Create Touch");
			
			gameObject.AddComponent<LeanTouch>();
			
			Selection.activeGameObject = gameObject;
		}
		
		// Draw the whole inspector
		public override void OnInspectorGUI()
		{
			if (LeanTouch.Instances.Count > 1)
			{
				EditorGUILayout.Separator();

				EditorGUILayout.HelpBox("There is more than one active and enabled LeanTouch...", MessageType.Warning);
			}

			var touch = (LeanTouch)target;
			
			Separator();
			
			DrawSettings(touch);
			
			Separator();
			
			DrawFingers(touch);
			
			Separator();
			
			Repaint();
		}
		
		private void DrawSettings(LeanTouch touch)
		{
			DrawTitle("Settings");
			DrawDefault("TapThreshold");
			DrawDefault("SwipeThreshold");
			DrawDefault("ReferenceDpi");
			DrawDefault("GuiLayers");
			
			Separator();
			
			DrawDefault("RecordFingers");
			
			if (touch.RecordFingers == true)
			{
				BeginIndent();
					DrawDefault("RecordThreshold");
					DrawDefault("RecordLimit");
				EndIndent();
			}
			
			Separator();

			DrawDefault("SimulateMultiFingers");
			
			if (touch.SimulateMultiFingers == true)
			{
				BeginIndent();
					DrawDefault("PinchTwistKey");
					DrawDefault("MultiDragKey");
					DrawDefault("FingerTexture");
				EndIndent();
			}
		}

		private void DrawFingers(LeanTouch touch)
		{
			DrawTitle("Fingers");
			
			allFingers.Clear();
			allFingers.AddRange(LeanTouch.Fingers);
			allFingers.AddRange(LeanTouch.InactiveFingers);
			allFingers.Sort((a, b) => a.Index.CompareTo(b.Index));
			
			for (var i = 0; i < allFingers.Count; i++)
			{
				var finger   = allFingers[i];
				var progress = touch.TapThreshold > 0.0f ? finger.Age / touch.TapThreshold : 0.0f;
				var style    = GetFadingLabel(finger.Set, progress);
				
				if (style.normal.textColor.a > 0.0f)
				{
					var screenPosition = finger.ScreenPosition;
					
					EditorGUILayout.LabelField("#" + finger.Index + " x " + finger.TapCount + " (" + Mathf.FloorToInt(screenPosition.x) + ", " + Mathf.FloorToInt(screenPosition.y) + ") - " + finger.Age.ToString("0.0"), style);
				}
			}
		}

		private static void DrawVector2(string name, Vector2 xy)
		{
			var left   = Reserve();
			var middle = Reserve(ref left);
			var right  = Reserve(ref middle, middle.width / 2);
			
			EditorGUI.LabelField(left, name);
			EditorGUI.FloatField(middle, xy.x);
			EditorGUI.FloatField(right, xy.y);
		}

		private void Separator()
		{
			EditorGUILayout.Separator();
		}

		private void DrawTitle(string name)
		{
			EditorGUI.LabelField(Reserve(), name, EditorStyles.boldLabel);
		}

		private void DrawDefault(string name)
		{
			EditorGUI.BeginChangeCheck();
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
			}
			if (EditorGUI.EndChangeCheck() == true)
			{
				serializedObject.ApplyModifiedProperties();
			}
		}

		private void BeginIndent()
		{
			EditorGUI.indentLevel += 1;
		}

		private void EndIndent()
		{
			EditorGUI.indentLevel -= 1;
		}
		
		private static Rect Reserve(ref Rect rect, float rightWidth = 0.0f, float padding = 2.0f)
		{
			if (rightWidth == 0.0f)
			{
				rightWidth = rect.width - EditorGUIUtility.labelWidth;
			}
			
			var left  = rect; left.xMax -= rightWidth;
			var right = rect; right.xMin = left.xMax;
			
			left.xMax -= padding;
			
			rect = left;
			
			return right;
		}
		
		private static Rect Reserve(float height = 16.0f)
		{
			var rect = EditorGUILayout.BeginVertical();
			{
				EditorGUILayout.LabelField("", GUILayout.Height(height));
			}
			EditorGUILayout.EndVertical();
			
			return rect;
		}
	}
}