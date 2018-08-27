// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
//
// Copyright (c) 2018 Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Creator Agreement, located
// here: https://id.magicleap.com/creator-terms
//
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.XR.MagicLeap
{
    /// <summary> 
    /// Custom attribute to make it easy to turn enum fields into bit masks in
    /// the inspector. The enum type must be defined in order for the inspector
    /// to be able to know what the bits should be set to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class BitMask : PropertyAttribute
    {
        /// <summary>
        /// The Type of the Enum that is being turned into a bit mask.
        /// </summary>
        public Type PropertyType;

        /// <summary> 
        /// Creates a new instance of BitMask with the passed in
        /// enum Type. This constructor call is automatic when
        /// decorating a field with this Attribute.
        /// </summary>
        /// <param name="propertyType">The Type value of the enum</param>
        public BitMask(Type propertyType)
        {
            PropertyType = propertyType;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(BitMask))]
    public class BitMaskPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Type propertyType = (attribute as BitMask).PropertyType;

            string[] enumNames = Enum.GetNames(propertyType);
            int[] enumValues = (int[]) Enum.GetValues(propertyType);

            int curIntValue = property.intValue;
            int curMaskValue = 0;

            for (int index = 0; index < enumValues.Length; ++index)
            {
                if ((curIntValue & enumValues[index]) == enumValues[index])
                {
                    curMaskValue |= 1 << index;
                }
            }

            // Draw the field using the built in MaskField functionality
            // However, since MaskField has no reference to the System.Type
            // of our enum, the value that is returned will not be shifted
            int newMaskValue = EditorGUI.MaskField(position, label, curMaskValue, enumNames);

            // Reset the current value
            curIntValue = 0;

            // Go through each value in the new mask and set the correct bit
            for (int index = 0; index < enumValues.Length; ++index)
            {
                if ((newMaskValue & (1 << index)) == (1 << index))
                {
                    curIntValue |= enumValues[index];
                }
            }

            // Make sure to set the value of the property in the end
            property.intValue = curIntValue;
        }
    }
#endif
}
