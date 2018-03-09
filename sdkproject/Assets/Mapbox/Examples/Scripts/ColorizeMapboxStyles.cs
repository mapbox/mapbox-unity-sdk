using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ColorizeMapboxStyles : MonoBehaviour {
    
    public Color m_baseColor;
    public Color m_detailColor1;
    public Color m_detailColor2;

    [HideInInspector]
    public bool m_isBaseColor_Overridden;
    [HideInInspector]
    public bool m_isDetailColor1_Overridden;
    [HideInInspector]
    public bool m_isDetailColor2_Overridden;

    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;

    private Color[] m_palette;
    private int m_baseIndex;

    private const string BASE_COLOR_NAME = "_BaseColor";
    private const string DETAIL_ONE_COLOR_NAME = "_DetailColor1";
    private const string DETAIL_TWO_COLOR_NAME = "_DetailColor2";

    private int m_baseColorId;
    private int m_detailOneColorId;
    private int m_detailTWoColorId;


    void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
        _renderer = GetComponent<Renderer>();

        m_baseColorId = Shader.PropertyToID(BASE_COLOR_NAME);
        m_detailOneColorId = Shader.PropertyToID(DETAIL_ONE_COLOR_NAME);
        m_detailTWoColorId = Shader.PropertyToID(DETAIL_TWO_COLOR_NAME);
    }

    public void SetPalette(Color[] palette)
    {
        m_palette = new Color[palette.Length];
        for (int i = 0; i < palette.Length; i++)
        {
            m_palette[i] = palette[i];
        }
    }

	private void Start()
	{
        if(m_baseColor == null)
        {
            Debug.Log("AHAHAIAI");
        }
        SetRandomColor();
	}

    public void SetColorValues()
    {
        _renderer.GetPropertyBlock(_propBlock);

        _propBlock.SetColor(m_baseColorId, m_baseColor);
        _propBlock.SetColor(m_detailOneColorId, m_detailColor1);
        _propBlock.SetColor(m_detailTWoColorId, m_detailColor2);

        _renderer.SetPropertyBlock(_propBlock);
    }

    private Color GetRandomColorFromPalette()
    {
        Color color = Color.white;
        if(m_palette.Length > 0)
        {
            color = m_palette[Random.Range(0, m_palette.Length)];
        }
        return color;
    }

    public void SetRandomColor()
    {
        m_baseColor = (m_isBaseColor_Overridden) ? m_baseColor : GetRandomColorFromPalette();
        m_detailColor1 = (m_isDetailColor1_Overridden) ? m_detailColor1 : GetRandomColorFromPalette();
        m_detailColor2 = (m_isDetailColor2_Overridden) ? m_detailColor2 : GetRandomColorFromPalette();
        SetColorValues();
    }
}
