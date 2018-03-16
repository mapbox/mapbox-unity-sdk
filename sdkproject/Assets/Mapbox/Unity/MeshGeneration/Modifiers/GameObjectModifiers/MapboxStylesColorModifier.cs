namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine.Assertions;
	using UnityEngine;

	public class MapboxStylesColorModifier : GameObjectModifier
	{

		public ScriptablePalette m_scriptablePalette;

		private const string _BASE_COLOR_NAME = "_BaseColor";
		private const string _DETAIL_ONE_COLOR_NAME = "_DetailColor1";
		private const string _DETAIL_TWO_COLOR_NAME = "_DetailColor2";

		private int _baseColorId;
		private int _detailOneColorId;
		private int _detailTWoColorId;

		public override void Initialize()
		{
			if (m_scriptablePalette == null)
			{
				return;
			}

			_baseColorId = Shader.PropertyToID(_BASE_COLOR_NAME);
			_detailOneColorId = Shader.PropertyToID(_DETAIL_ONE_COLOR_NAME);
			_detailTWoColorId = Shader.PropertyToID(_DETAIL_TWO_COLOR_NAME);
		}

		private Color GetRandomColorFromPalette()
		{
			Color color = Color.white;
			if (m_scriptablePalette.m_colors.Length > 0)
			{
				color = m_scriptablePalette.m_colors[Random.Range(0, m_scriptablePalette.m_colors.Length)];
			}
			return color;
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			if (m_scriptablePalette == null)
			{
				return;
			}

			MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

			ve.MeshRenderer.GetPropertyBlock(propBlock);

			Color baseColor = (m_scriptablePalette.m_setBaseColor_Override) ? m_scriptablePalette.m_baseColor_Override : GetRandomColorFromPalette();
			Color detailColor1 = (m_scriptablePalette.m_setDetailColor1_Override) ? m_scriptablePalette.m_detailColor1_Override : GetRandomColorFromPalette();
			Color detailColor2 = (m_scriptablePalette.m_setDetailColor2_Override) ? m_scriptablePalette.m_detailColor2_Override : GetRandomColorFromPalette();

			propBlock.SetColor(_baseColorId, baseColor);
			propBlock.SetColor(_detailOneColorId, detailColor1);
			propBlock.SetColor(_detailTWoColorId, detailColor2);

			ve.MeshRenderer.SetPropertyBlock(propBlock);
		}
	}
}
