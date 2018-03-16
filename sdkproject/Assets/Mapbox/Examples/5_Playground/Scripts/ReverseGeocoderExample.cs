//-----------------------------------------------------------------------
// <copyright file="ForwardGeocoderExample.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Examples.Playground
{
    using Mapbox.Json;
    using Mapbox.Utils.JsonConverters;
    using UnityEngine;
    using UnityEngine.UI;

    public class ReverseGeocoderExample : MonoBehaviour
    {
        [SerializeField]
        ReverseGeocodeUserInput _searchLocation;

        [SerializeField]
        Text _resultsText;

        void Awake()
        {
            _searchLocation.OnGeocoderResponse += SearchLocation_OnGeocoderResponse;
        }

        void OnDestroy()
        {
            if (_searchLocation != null)
            {
                _searchLocation.OnGeocoderResponse -= SearchLocation_OnGeocoderResponse;
            }
        }

        void SearchLocation_OnGeocoderResponse(object sender, System.EventArgs e)
        {
            _resultsText.text = JsonConvert.SerializeObject(_searchLocation.Response, Formatting.Indented, JsonConverters.Converters);
        }
    }
}