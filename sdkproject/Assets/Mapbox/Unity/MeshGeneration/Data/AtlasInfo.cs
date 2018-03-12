using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mapbox.Unity.MeshGeneration.Data
{
	[Serializable]
	public class AtlasEntity
	{
		public Rect TextureRect;
		public int MidFloorCount;
		public float ColumnCount;

		public float TopSectionRatio;
		public float BottomSectionRatio;

		public int PreferredEdgeSectionLength = 10;
		public float FloorHeight;
		public float FirstFloorHeight;
		public float TopFloorHeight;
	}

	[CreateAssetMenu(menuName = "Mapbox/AtlasInfo")]
	public class AtlasInfo : ScriptableObject
	{
		public List<AtlasEntity> Textures;
		public List<AtlasEntity> Roofs;

        private UnityEvent m_OnValidate = new UnityEvent();

        public void AddOnValidateEvent(UnityAction action)
        {
            m_OnValidate.AddListener(action);
        }

        private void OnValidate()
        {
            if(m_OnValidate != null)
            {
                m_OnValidate.Invoke();
            }
        }
	}
}
