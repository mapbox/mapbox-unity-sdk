﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.MeshGeneration.Modifiers;
using System;
using Mapbox.Unity.Map;

public class ColliderOptions : ModifierProperties
{
	public override Type ModifierType
	{
		get
		{
			return typeof(ColliderModifier);
		}
	}

	public ColliderType colliderType = ColliderType.None;
}
