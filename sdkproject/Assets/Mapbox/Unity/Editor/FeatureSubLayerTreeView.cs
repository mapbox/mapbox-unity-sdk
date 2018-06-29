namespace Mapbox.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor.IMGUI.Controls;
	using UnityEditor;
	using Mapbox.Unity.Map;

	internal class FeatureSubLayerTreeView : TreeViewWithTreeModel<FeatureTreeElement>
	{
		public SerializedProperty Layers;
		private float kToggleWidth = 18f;
		public static int uniqueId = 3000;
		const float kRowHeights = 20f;
		const float nameOffset = 15f;
		private GUIStyle columnStyle = new GUIStyle() 
		{
			alignment = TextAnchor.MiddleLeft, 
			normal = new GUIStyleState(){textColor = Color.white} 
		};

		public FeatureSubLayerTreeView(TreeViewState state, MultiColumnHeader multicolumnHeader, TreeModel<FeatureTreeElement> model) : base(state, multicolumnHeader, model)
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
			// Only allow rename if we can show the rename overlay with a certain width (label might be clipped by other columns)
			Rect renameRect = GetRenameRect(treeViewRect, 0, item);
			return renameRect.width > 30;
		}

		protected override void RenameEnded(RenameEndedArgs args)
		{
			if (Layers == null || Layers.arraySize == 0)
			{
				return;
			}

			if (args.acceptedRename)
			{
				var element = treeModel.Find(args.itemID);
				element.name = string.IsNullOrEmpty(args.newName.Trim()) ? args.originalName : args.newName;
				var layer = Layers.GetArrayElementAtIndex(args.itemID - uniqueId);
				layer.FindPropertyRelative("coreOptions.sublayerName").stringValue = element.name;
				Reload();
			}
		}

		protected override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
		{
			Rect cellRect = GetCellRectForTreeFoldouts(rowRect);
			cellRect.xMin = nameOffset;
			CenterRectUsingSingleLineHeight(ref cellRect);
			return base.GetRenameRect(cellRect, row, item);
		}

		public void RemoveItemFromTree(int id)
		{
			treeModel.RemoveElements(new List<int>() { id });
		}

		protected override void RowGUI(RowGUIArgs args)		
		{
			var rowItem = (TreeViewItem <FeatureTreeElement>)args.item;
			for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
			{
				CellGUI(args.GetCellRect(i), rowItem, (MyColumns)args.GetColumn(i), ref args);
			}
		}

		void CellGUI(Rect cellRect, TreeViewItem<FeatureTreeElement> item, MyColumns column, ref RowGUIArgs args)
		{
			// Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
			if (Layers == null || Layers.arraySize == 0)
			{
				return;
			}

			if (Layers.arraySize <= args.item.id - uniqueId)
			{
				return;
			}

			var layer = Layers.GetArrayElementAtIndex(args.item.id - uniqueId);
			CenterRectUsingSingleLineHeight(ref cellRect);
			if (column == MyColumns.Name)
			{
				layer.FindPropertyRelative("coreOptions.isActive").boolValue = item.data.isActive;
				Rect toggleRect = cellRect;
				toggleRect.x += GetContentIndent(item);
				toggleRect.width = kToggleWidth;

				if (toggleRect.xMax < cellRect.xMax)
				{
					item.data.isActive = EditorGUI.Toggle(toggleRect, item.data.isActive); // hide when outside cell rect
				}

				cellRect.xMin += nameOffset; // Adding some gap between the checkbox and the name
				args.rowRect = cellRect;

				layer.FindPropertyRelative("coreOptions.sublayerName").stringValue = item.data.Name;
				//This draws the name property
				base.RowGUI(args);
			}
			if (column == MyColumns.Type)
			{
				//var typeString = ((PresetFeatureType)cellItem.FindPropertyRelative("presetFeatureType").intValue).ToString();
				//item.data.Type = typeString;
				cellRect.xMin += 15f; // Adding some gap between the checkbox and the name
				EditorGUI.LabelField(cellRect, item.data.Type, columnStyle);
			}
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
					autoResize = true,
					canSort = false
				},

				//Type column
				new MultiColumnHeaderState.Column
				{
					headerContent = new GUIContent("Type"),
					contextMenuText = "Type",
					headerTextAlignment = TextAlignment.Center,
					autoResize = true,
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
