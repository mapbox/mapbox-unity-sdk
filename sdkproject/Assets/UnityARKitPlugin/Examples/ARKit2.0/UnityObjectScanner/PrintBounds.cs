using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.XR.iOS
{
    [RequireComponent(typeof(UnityEngine.UI.Text))]
    public class PrintBounds : MonoBehaviour
    {
        [SerializeField]
        PickBoundingBox m_Picker;

        Text m_BoundsText;

        void Start()
        {
            m_BoundsText = GetComponent<Text>();
        }

        // Update is called once per frame
        void Update()
        {
            var bounds = m_Picker.bounds;
			m_BoundsText.text = string.Format("Bounds:{0}", bounds.ToString("F2")) + string.Format(",size={0} ", bounds.size.ToString("F2"));
			m_BoundsText.text += string.Format ("Transform.pos:{0} rot:{1}", m_Picker.transform.position.ToString ("F2"), m_Picker.transform.rotation.ToString ("F2"));
        }
    }
}
