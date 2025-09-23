using System;
using UnityEngine;

namespace Mapbox.BaseModule.Utilities.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class GameObjectLayerAttribute : PropertyAttribute
    {
    }
}