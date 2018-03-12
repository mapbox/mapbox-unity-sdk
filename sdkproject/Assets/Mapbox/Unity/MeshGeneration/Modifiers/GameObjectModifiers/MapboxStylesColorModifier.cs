namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using Mapbox.Unity.MeshGeneration.Data;
    using UnityEngine.Assertions;
    using UnityEngine;

    public class MapboxStylesColorModifier : GameObjectModifier
    {

        public ScriptablePalette m_scriptablePalette;

        private const string BASE_COLOR_NAME = "_BaseColor";
        private const string DETAIL_ONE_COLOR_NAME = "_DetailColor1";
        private const string DETAIL_TWO_COLOR_NAME = "_DetailColor2";

        private int m_baseColorId;
        private int m_detailOneColorId;
        private int m_detailTWoColorId;

        public override void Initialize()
        {
            Assert.IsNotNull(m_scriptablePalette, "No scriptable palette assigned.");
            Assert.IsTrue(m_scriptablePalette.m_colors.Length > 0, "No color palette defined in scriptable palette.");

            m_baseColorId = Shader.PropertyToID(BASE_COLOR_NAME);
            m_detailOneColorId = Shader.PropertyToID(DETAIL_ONE_COLOR_NAME);
            m_detailTWoColorId = Shader.PropertyToID(DETAIL_TWO_COLOR_NAME);
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
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

            ve.MeshRenderer.GetPropertyBlock(propBlock);

            Color[] colors = m_scriptablePalette.m_colors;

            Color baseColor = (m_scriptablePalette.m_setBaseColor_Override) ? m_scriptablePalette.m_baseColor_Override : GetRandomColorFromPalette();
            Color detailColor1 = (m_scriptablePalette.m_setDetailColor1_Override) ? m_scriptablePalette.m_detailColor1_Override : GetRandomColorFromPalette();
            Color detailColor2 = (m_scriptablePalette.m_setDetailColor2_Override) ? m_scriptablePalette.m_detailColor2_Override : GetRandomColorFromPalette();

            propBlock.SetColor(m_baseColorId, baseColor);
            propBlock.SetColor(m_detailOneColorId, detailColor1);
            propBlock.SetColor(m_detailTWoColorId, detailColor2);

            ve.MeshRenderer.SetPropertyBlock(propBlock);
        }
    }
}
