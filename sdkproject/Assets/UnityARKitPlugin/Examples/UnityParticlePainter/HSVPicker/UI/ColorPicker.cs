﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;

public class ColorPicker : MonoBehaviour
{
    private float _hue = 0;
    private float _saturation = 0;
    private float _brightness = 0;

    private float _red = 0;
    private float _green = 0;
    private float _blue = 0;

    private float _alpha = 1;

    public ColorChangedEvent onValueChanged = new ColorChangedEvent();
    public HSVChangedEvent onHSVChanged = new HSVChangedEvent();

    public Color CurrentColor
    {
        get
        {
            return new Color(_red, _green, _blue, _alpha);
        }
        set
        {
            if (CurrentColor == value)
                return;

            _red = value.r;
            _green = value.g;
            _blue = value.b;
            _alpha = value.a;

            RGBChanged();
            
            SendChangedEvent();
        }
    }

    private void Start()
    {
        SendChangedEvent();
    }

    public float H
    {
        get
        {
            return _hue;
        }
        set
        {
            if (_hue == value)
                return;

            _hue = value;

            HSVChanged();

            SendChangedEvent();
        }
    }

    public float S
    {
        get
        {
            return _saturation;
        }
        set
        {
            if (_saturation == value)
                return;

            _saturation = value;

            HSVChanged();

            SendChangedEvent();
        }
    }

    public float V
    {
        get
        {
            return _brightness;
        }
        set
        {
            if (_brightness == value)
                return;

            _brightness = value;

            HSVChanged();

            SendChangedEvent();
        }
    }

    public float R
    {
        get
        {
            return _red;
        }
        set
        {
            if (_red == value)
                return;

            _red = value;

            RGBChanged();

            SendChangedEvent();
        }
    }

    public float G
    {
        get
        {
            return _green;
        }
        set
        {
            if (_green == value)
                return;

            _green = value;

            RGBChanged();

            SendChangedEvent();
        }
    }

    public float B
    {
        get
        {
            return _blue;
        }
        set
        {
            if (_blue == value)
                return;

            _blue = value;

            RGBChanged();

            SendChangedEvent();
        }
    }

    private float A
    {
        get
        {
            return _alpha;
        }
        set
        {
            if (_alpha == value)
                return;

            _alpha = value;

            SendChangedEvent();
        }
    }

    private void RGBChanged()
    {
        HsvColor color = HSVUtil.ConvertRgbToHsv(CurrentColor);

        _hue = color.normalizedH;
        _saturation = color.normalizedS;
        _brightness = color.normalizedV;
    }

    private void HSVChanged()
    {
        Color color = HSVUtil.ConvertHsvToRgb(_hue * 360, _saturation, _brightness, _alpha);

        _red = color.r;
        _green = color.g;
        _blue = color.b;
    }

    private void SendChangedEvent()
    {
        onValueChanged.Invoke(CurrentColor);
        onHSVChanged.Invoke(_hue, _saturation, _brightness);
    }

    public void AssignColor(ColorValues type, float value)
    {
        switch (type)
        {
            case ColorValues.R:
                R = value;
                break;
            case ColorValues.G:
                G = value;
                break;
            case ColorValues.B:
                B = value;
                break;
            case ColorValues.A:
                A = value;
                break;
            case ColorValues.Hue:
                H = value;
                break;
            case ColorValues.Saturation:
                S = value;
                break;
            case ColorValues.Value:
                V = value;
                break;
            default:
                break;
        }
    }

    public float GetValue(ColorValues type)
    {
        switch (type)
        {
            case ColorValues.R:
                return R;
            case ColorValues.G:
                return G;
            case ColorValues.B:
                return B;
            case ColorValues.A:
                return A;
            case ColorValues.Hue:
                return H;
            case ColorValues.Saturation:
                return S;
            case ColorValues.Value:
                return V;
            default:
                throw new System.NotImplementedException("");
        }
    }
}
