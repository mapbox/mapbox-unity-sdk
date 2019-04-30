namespace Mapbox.Editor
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor.IMGUI.Controls;
	using UnityEditor;
	using Mapbox.Unity.Map;

	public class PointsOfInterestSubLayerTreeView : TreeView
	{
		public SerializedProperty Layers;
		private float kToggleWidth = 18f;
		private const int uniqueId = 0;

		public bool hasChanged = false;

		public PointsOfInterestSubLayerTreeView(TreeViewState state)
			: base(state)
		{
			showAlternatingRowBackgrounds = true;
			showBorder = true;
			Reload();
		}

		protected override TreeViewItem BuildRoot()
		{
			// The root item is required to have a depth of -1, and the rest of the items increment from that.
			var root = new TreeViewItem { id = -1, depth = -1, displayName = "Root" };

			var items = new List<TreeViewItem>();
			var index = 0;

			if (Layers != null)
			{
				for (int i = 0; i < Layers.arraySize; i++)
				{
					var name = Layers.GetArrayElementAtIndex(i).FindPropertyRelative("coreOptions.sublayerName").stringValue;
					items.Add(new TreeViewItem { id = index + uniqueId, depth = 1, displayName = name });
					index++;
				}
			}

			// Utility method that initializes the TreeViewItem.children and .parent for all items.
			SetupParentsAndChildrenFromDepths(root, items);

			// Return root of the tree
			return root;
		}

		protected override bool CanRename(TreeViewItem item)
		{
			return true;
		}

		protected override void RenameEnded(RenameEndedArgs args)
		{
			if (Layers == null)
			{
				return;
			}

			var layer = Layers.GetArrayElementAtIndex(args.itemID - uniqueId);
			layer.FindPropertyRelative("coreOptions.sublayerName").stringValue = string.IsNullOrEmpty(args.newName.Trim()) ? args.originalName : args.newName;
		}

		protected override void RowGUI(RowGUIArgs args)
		{
			Rect toggleRect = args.rowRect;
			toggleRect.width = kToggleWidth;
			var item = Layers.GetArrayElementAtIndex(args.item.id - uniqueId);
			EditorGUI.BeginChangeCheck();
			item.FindPropertyRelative("coreOptions.isActive").boolValue = EditorGUI.Toggle(toggleRect, item.FindPropertyRelative("coreOptions.isActive").boolValue);
			if(EditorGUI.EndChangeCheck())
			{
				hasChanged = true;
			}
			args.item.displayName = item.FindPropertyRelative("coreOptions.sublayerName").stringValue;
			base.RowGUI(args);
		}
	}
}
