namespace Mapbox.Unity.MeshGeneration.Modifiers
{
    using Mapbox.Unity.MeshGeneration.Data;
    using UnityEngine.Assertions;

    public class AddColorizeMapboxStyles : GameObjectModifier
    {

        public ScriptablePalette m_scriptablePalette;

        public override void Run(VectorEntity ve, UnityTile tile)
        {
            Assert.IsNotNull(m_scriptablePalette, "No scriptable palette assigned.");
            Assert.IsTrue(m_scriptablePalette.m_colors.Length > 0, "No color palette defined in scriptable palette.");
            ColorizeMapboxStyles colorize = ve.GameObject.AddComponent<ColorizeMapboxStyles>();
            colorize.SetPalette(m_scriptablePalette.m_colors);
            if(m_scriptablePalette.m_setBaseColor_Override)
            {
                colorize.m_baseColor = m_scriptablePalette.m_baseColor_Override;
                colorize.m_isBaseColor_Overridden = true;
            }
            if (m_scriptablePalette.m_setDetailColor1_Override)
            {
                colorize.m_detailColor1 = m_scriptablePalette.m_detailColor1_Override;
                colorize.m_isDetailColor1_Overridden = true;
            }
            if (m_scriptablePalette.m_setDetailColor2_Override)
            {
                colorize.m_detailColor2 = m_scriptablePalette.m_detailColor2_Override;
                colorize.m_isDetailColor2_Overridden = true;
            }
        }
    }
}
