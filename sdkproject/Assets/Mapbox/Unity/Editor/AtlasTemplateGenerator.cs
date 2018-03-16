using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.MeshGeneration.Data;
using System.IO;

namespace Mapbox.Editor
{

	class AtlasTemplateGenerator : EditorWindow
	{
		[MenuItem("Mapbox/Atlas Template Generator")]
		public static void ShowWindow()
		{
			EditorWindow.GetWindow(typeof(AtlasTemplateGenerator));
		}

		public class PixelRect
		{
			public int x;
			public int y;

			public int xx;
			public int yy;
		}

		public string m_saveFileName = "AtlasTemplate";

		public Texture2D m_texture;

		public AtlasInfo m_atlasInfo;

		public Color[] m_colors;

		public int m_textureResolution = 2048;

		public bool m_generateFacadesTemplate = true;
		public bool m_generateRoofsTemplate = false;

		private int _drawCount;

		private const int _DEFAULT_TEX_SIZE = 2048;
		private const int _MIN_TEX_SIZE = 512;
		private const int _MAX_TEX_SIZE = 2048;

		private const float _cellRatioMargin = 0.01f;

		private void Awake()
		{
			CreateTexture();
		}

		private void CreateTexture()
		{
			m_texture = new Texture2D(m_textureResolution, m_textureResolution, TextureFormat.ARGB32, false);
			m_texture.filterMode = FilterMode.Point;
		}

		void OnGUI()
		{
			GUILayout.Space(20);

			GUIStyle titleStyle = new GUIStyle(EditorStyles.label);

			titleStyle.fontSize = 32;
			titleStyle.normal.textColor = Color.white;
			titleStyle.fontStyle = FontStyle.Bold;

			titleStyle.stretchWidth = true;
			titleStyle.stretchHeight = true;

			titleStyle.alignment = TextAnchor.MiddleCenter;

			GUILayout.BeginVertical();

			EditorGUILayout.LabelField("Mapbox Atlas Template Generator", titleStyle, GUILayout.Height(100));

			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical(GUILayout.MinWidth(200), GUILayout.MaxWidth(300));

			EditorGUI.BeginChangeCheck();

			EditorGUI.indentLevel++;

			m_atlasInfo = EditorGUILayout.ObjectField("Atlas info:", m_atlasInfo, typeof(AtlasInfo), true) as Mapbox.Unity.MeshGeneration.Data.AtlasInfo;

			EditorGUILayout.Space();

			m_generateFacadesTemplate = EditorGUILayout.Toggle("Create Facades", m_generateFacadesTemplate);
			m_generateRoofsTemplate = EditorGUILayout.Toggle("Create Roofs", m_generateRoofsTemplate);

			if (EditorGUI.EndChangeCheck())
			{
				if (m_atlasInfo != null)
				{
					int facadeCount = m_generateFacadesTemplate ? m_atlasInfo.Textures.Count : 0;
					int roofCount = m_generateRoofsTemplate ? m_atlasInfo.Roofs.Count : 0;

					int textureCount = facadeCount + roofCount;

					m_colors = new Color[textureCount];

					float hueIncrement = (float)1.0f / textureCount;
					float hue = 0.0f;

					for (int i = 0; i < textureCount; i++)
					{
						m_colors[i] = Color.HSVToRGB(hue, 1.0f, 1.0f);
						hue += hueIncrement;
					}
				}
				else
				{
					m_colors = new Color[0];
					CreateTexture();
				}
			}

			EditorGUI.BeginChangeCheck();

			m_textureResolution = Mathf.Clamp(EditorGUILayout.IntField("Texture resolution:", m_textureResolution), _MIN_TEX_SIZE, _MAX_TEX_SIZE);

			if (EditorGUI.EndChangeCheck())
			{
				CreateTexture();
			}

			EditorGUILayout.Space();

			if (m_colors != null)
			{
				for (int i = 0; i < m_colors.Length; i++)
				{
					string colorFieldName = string.Format("Color {0}", i);
					m_colors[i] = EditorGUILayout.ColorField(colorFieldName, m_colors[i]);
				}
			}

			if (GUILayout.Button("Generate Template"))
			{
				if (m_atlasInfo == null)
				{
					EditorUtility.DisplayDialog("Atlas Template Generator", "Error: No AtlasInfo object selected.", "Ok");
					return;
				}
				if (!m_generateFacadesTemplate && !m_generateRoofsTemplate)
				{
					EditorUtility.DisplayDialog("Atlas Template Generator", "Error: Template generation requires Create Facades and/or Create Roofs to be enabled.", "Ok");
					return;
				}
				GenerateTemplate();
			}

			EditorGUILayout.Space();

			if (GUILayout.Button("Save to file"))
			{
				SaveTextureAsPNG();
			}

			EditorGUI.indentLevel--;

			GUILayout.EndVertical();

			GUIStyle boxStyle = new GUIStyle();

			boxStyle.alignment = TextAnchor.UpperLeft;

			GUILayout.Box(m_texture, boxStyle, GUILayout.Width(300), GUILayout.Height(300), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

			GUILayout.EndHorizontal();
			GUILayout.EndVertical();

			GUILayout.Space(20);
		}

		private int GetPixelCoorFromAtlasRatio(float ratio)
		{
			return (int)(m_textureResolution * ratio);
		}

		private PixelRect ConvertUVRectToPixelRect(Rect atlasRect)
		{
			PixelRect pixelRect = new PixelRect();
			pixelRect.x = GetPixelCoorFromAtlasRatio(atlasRect.x);
			pixelRect.y = GetPixelCoorFromAtlasRatio(atlasRect.y);
			pixelRect.xx = GetPixelCoorFromAtlasRatio(atlasRect.x + atlasRect.width);
			pixelRect.yy = GetPixelCoorFromAtlasRatio(atlasRect.y + atlasRect.height);
			return pixelRect;
		}

		private void DrawRect(PixelRect pr, Color color)
		{
			for (int i = pr.x; i < pr.xx; i++)
			{
				for (int j = pr.y; j < pr.yy; j++)
				{
					m_texture.SetPixel(i, j, color);
				}
			}
		}

		private void DrawWatermark(int x, int y)
		{
			m_texture.SetPixel(x, y, Color.black);
			m_texture.SetPixel(x + 3, y, Color.black);
			m_texture.SetPixel(x, y + 3, Color.black);
			m_texture.SetPixel(x + 3, y + 3, Color.black);

			m_texture.SetPixel(x + 1, y + 1, Color.black);
			m_texture.SetPixel(x + 2, y + 1, Color.black);
			m_texture.SetPixel(x + 1, y + 2, Color.black);
			m_texture.SetPixel(x + 2, y + 2, Color.black);
		}

		private void DrawCornerWatermarks(PixelRect pr)
		{
			DrawWatermark(pr.x, pr.y);
			DrawWatermark(pr.xx - 4, pr.y);
			DrawWatermark(pr.x, pr.yy - 4);
			DrawWatermark(pr.xx - 4, pr.yy - 4);
		}

		private void DrawDebugCross(PixelRect pr)
		{
			int centerX = (pr.x + pr.xx) / 2;
			int centerY = (pr.y + pr.yy) / 2;

			m_texture.SetPixel(centerX, centerY, Color.black);

			for (int x = pr.x; x < pr.xx; x++)
			{
				m_texture.SetPixel(x, centerY, Color.black);
				m_texture.SetPixel(x, centerY - 1, Color.black);
				m_texture.SetPixel(x, centerY + 1, Color.black);
			}

			for (int y = pr.y; y < pr.yy; y++)
			{
				m_texture.SetPixel(centerX, y, Color.black);
				m_texture.SetPixel(centerX - 1, y, Color.black);
				m_texture.SetPixel(centerX + 1, y, Color.black);
			}
		}

		private void DrawAtlasEntityData(List<AtlasEntity> aeList)
		{
			for (int i = 0; i < aeList.Count; i++)
			{
				AtlasEntity ae = aeList[i];

				Rect baseRect = ae.TextureRect;

				float topRatio = ae.TopSectionRatio * baseRect.height;
				float bottomRatio = ae.BottomSectionRatio * baseRect.height;
				float middleRatio = baseRect.height - (topRatio + bottomRatio);

				Rect groundFloorRect = new Rect(baseRect.x, baseRect.y, baseRect.width, bottomRatio);
				Rect topFloorRect = new Rect(baseRect.x, baseRect.y + baseRect.height - topRatio, baseRect.width, topRatio);

				PixelRect basePixelRect = ConvertUVRectToPixelRect(baseRect);
				PixelRect groundFloorPixelRect = ConvertUVRectToPixelRect(groundFloorRect);
				PixelRect topFloorPixelRect = ConvertUVRectToPixelRect(topFloorRect);

				Color color = m_colors[_drawCount];
				Color colorLight = (color + Color.white) / 2;
				Color colorDark = (color + Color.black) / 2;

				DrawRect(basePixelRect, color);
				DrawRect(groundFloorPixelRect, colorLight);
				DrawRect(topFloorPixelRect, colorDark);

				DrawDebugCross(groundFloorPixelRect);
				DrawDebugCross(topFloorPixelRect);

				int numColumns = (int)ae.ColumnCount;
				int numMidFloors = ae.MidFloorCount;

				float colWidth = baseRect.width / numColumns;
				float floorHeight = middleRatio / numMidFloors;

				float midFloorBase = baseRect.y + bottomRatio;

				float mrgn = _cellRatioMargin;
				float halfMrgn = mrgn / 2;

				for (int j = 0; j < numMidFloors; j++)
				{
					float floorStart = midFloorBase + (floorHeight * j);

					for (int k = 0; k < numColumns; k++)
					{
						float columnStart = baseRect.x + (colWidth * k);

						Rect cellRect = new Rect(columnStart + halfMrgn, floorStart + halfMrgn, colWidth - mrgn, floorHeight - mrgn);
						PixelRect cellPixelRect = ConvertUVRectToPixelRect(cellRect);

						DrawRect(cellPixelRect, Color.white);
						DrawDebugCross(cellPixelRect);
					}
				}
				DrawCornerWatermarks(groundFloorPixelRect);
				DrawCornerWatermarks(topFloorPixelRect);
				_drawCount++;
			}
		}

		public void GenerateTemplate()
		{
			_drawCount = 0;
			if (m_generateFacadesTemplate)
			{
				DrawAtlasEntityData(m_atlasInfo.Textures);
			}
			if (m_generateRoofsTemplate)
			{
				DrawAtlasEntityData(m_atlasInfo.Roofs);
			}
			m_texture.Apply();
		}

		public void SaveTextureAsPNG()
		{
			var path = EditorUtility.SaveFilePanel("Save texture as PNG", "Assets", "AtlasTemplate.png", "png");
			if (path.Length == 0)
			{
				return;
			}
			byte[] pngData = m_texture.EncodeToPNG();
			if (pngData != null)
			{
				File.WriteAllBytes(path, pngData);
				Debug.Log(pngData.Length / 1024 + "Kb was saved as: " + path);
				AssetDatabase.Refresh();
			}
		}
	}
}