namespace Mapbox.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor.IMGUI.Controls;
	using UnityEditor;
	using Mapbox.Unity.Map;

	internal class FeatureSubLayerTreeView : TreeViewWithTreeModel<MyTreeElement>
	{
		public SerializedProperty Layers;
		private float kToggleWidth = 18f;
		public static int uniqueId = 3000;
		const float kRowHeights = 20f;

		public FeatureSubLayerTreeView(TreeViewState state, MultiColumnHeader multicolumnHeader, TreeModel<MyTreeElement> model) : base(state, multicolumnHeader, model)
		{
			// Custom setup
			rowHeight = kRowHeights;
			showAlternatingRowBackgrounds = true;
			showBorder = true;
			customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI
			extraSpaceBeforeIconAndLabel = kToggleWidth;

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
			var rowItem = (TreeViewItem <MyTreeElement>)args.item;
			for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
			{
				CellGUI(args.GetCellRect(i), rowItem, (MyColumns)args.GetColumn(i), ref args);
			}
		}


		void CellGUI(Rect cellRect, TreeViewItem<MyTreeElement> item, MyColumns column, ref RowGUIArgs args)
		{
			// Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
			CenterRectUsingSingleLineHeight(ref cellRect);
			// Do toggle
			Rect toggleRect = cellRect;
			toggleRect.x += GetContentIndent(item);
			toggleRect.width = kToggleWidth;

			args.rowRect = cellRect;

			cellRect.xMin += 5f; // When showing controls make some extra spacing
								 //var cellItem = Layers.GetArrayElementAtIndex(args.item.id - uniqueId);

			if (column == MyColumns.Name)
			{
				item.data.isActive = EditorGUI.Toggle(toggleRect, item.data.isActive);
				//cellItem.FindPropertyRelative("coreOptions.isActive").boolValue = item.data.isActive;
				//cellItem.FindPropertyRelative("coreOptions.sublayerName").stringValue  = item.data.Name;
				//args.item.displayName = item.data.Name;
				EditorGUILayout.TextField(item.data.Type);
				Debug.Log("name");
			}
			if (column == MyColumns.Type)
			{
				//var typeString = ((PresetFeatureType)cellItem.FindPropertyRelative("presetFeatureType").intValue).ToString();
				//item.data.Type = typeString;
				EditorGUILayout.LabelField(item.data.Type);
				Debug.Log("type");

			}
			base.RowGUI(args);
		}

		// All columns
		enum MyColumns
		{
			Name,
			Type
		}

		public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
		{
			var columns = new[]
			{
				//Name column
				new MultiColumnHeaderState.Column
				{
					headerContent = new GUIContent("Name"),
					contextMenuText = "Name",
					headerTextAlignment = TextAlignment.Center,
					//sortedAscending = true,
					//sortingArrowAlignment = TextAlignment.Right,
					//width = 30,
					//minWidth = 30,
					//maxWidth = 60,
					autoResize = false,
					allowToggleVisibility = true,
					canSort = false
				},

				//Type column
				new MultiColumnHeaderState.Column
				{
					headerContent = new GUIContent("Type"),
					contextMenuText = "Type",
					headerTextAlignment = TextAlignment.Center,
					//sortedAscending = true,
					//sortingArrowAlignment = TextAlignment.Right,
					//width = 30,
					//minWidth = 30,
					//maxWidth = 60,
					autoResize = false,
					allowToggleVisibility = true,
					canSort = false
				}
			};

			var state = new MultiColumnHeaderState(columns);
			return state;
		}
	}
}
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
		private const int uniqueId = 0;//100000;

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
			item.FindPropertyRelative("coreOptions.isActive").boolValue = EditorGUI.Toggle(toggleRect, item.FindPropertyRelative("coreOptions.isActive").boolValue);
			args.item.displayName = item.FindPropertyRelative("coreOptions.sublayerName").stringValue;
			base.RowGUI(args);
		}
	}
}
