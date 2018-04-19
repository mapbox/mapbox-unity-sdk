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

	public enum AtlasEntityType
	{
		TextureBased,
		MeshGenBased
	}

	[CreateAssetMenu(menuName = "Mapbox/AtlasInfo")]
	public class AtlasInfo : ScriptableObject
	{
		public List<AtlasEntity> Textures;
		public List<AtlasEntity> Roofs;

        private UnityEvent m_OnValidate = new UnityEvent();

		public AtlasEntityType AtlasEntityType;

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

			if(AtlasEntityType == AtlasEntityType.TextureBased)
			{
				foreach (var tex in Textures)
				{

					tex.FirstFloorHeight = tex.PreferredEdgeSectionLength * ((tex.TextureRect.height * tex.BottomSectionRatio) / tex.TextureRect.width);
					tex.TopFloorHeight = tex.PreferredEdgeSectionLength * ((tex.TextureRect.height * tex.TopSectionRatio) / tex.TextureRect.width);
					tex.FloorHeight = tex.PreferredEdgeSectionLength * ((1 - tex.TopSectionRatio - tex.BottomSectionRatio) * (tex.TextureRect.height / tex.TextureRect.width));
				}
			}
			else
			{
				foreach (var tex in Textures)
				{
					tex.BottomSectionRatio = (tex.FirstFloorHeight / tex.PreferredEdgeSectionLength) * tex.TextureRect.width / tex.TextureRect.height;
					tex.TopSectionRatio = (tex.TopFloorHeight / tex.PreferredEdgeSectionLength) * tex.TextureRect.width / tex.TextureRect.height;
				}
			}
			
        }
	}
}
