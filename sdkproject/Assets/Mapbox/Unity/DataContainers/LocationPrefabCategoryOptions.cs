namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using System.Collections.Generic;

	public static class LocationPrefabCategoryOptions
	{

		static LocationPrefabCategoryOptions()
		{
			PopulateCategoriesToMakiDictionary();
		}

		private static Dictionary<LocationPrefabCategories, List<string>> CategoriesToMakiDictionary = new Dictionary<LocationPrefabCategories, List<string>>
		{
			{LocationPrefabCategories.ArtsAndEntertainment,new List<string>{"art-gallery", "cinema", "stadium", "museum", "library", "zoo", "music", "theatre", "amusement-park"}},
			{LocationPrefabCategories.Food,new List<string>{"cafe", "bakery", "fast-food", "grocery", "ice-cream", "restaurant"}},
			{LocationPrefabCategories.Nightlife,new List<string>{"bar", "beer"}},
			{LocationPrefabCategories.OutdoorsAndRecreation,new List<string>{"aquarium", "campsite", "attraction", "castle", "cemetery", "dog-park", "drinking-water", "garden", "golf", "monument", "park", "picnic-site", "playground", "swimming"}},
			{LocationPrefabCategories.Services,new List<string>{"bank", "dentist", "toilet", "veterinary", "pharmacy", "college", "school", "hospital", "place-of-worship", "religious-christian", "religious-jewish", "religious-muslim", "police", "post", "doctor", "fire-station", "information", "town-hall", "prison", "embassy", "fuel", "laundry", "lodging"}},
			{LocationPrefabCategories.Shops,new List<string>{"alcohol-shop", "clothing-store", "shop"}},
			{LocationPrefabCategories.Transportation,new List<string>{"bus", "car", "bicycle-share", "bicycle", "airfield", "ferry", "harbor", "heliport"}},
		};

		private static Dictionary<string, LocationPrefabCategories> MakiToCategoriesDictionary = new Dictionary<string, LocationPrefabCategories>();


		//Creates a reverse reference from the CategoriesToMakiDictionary
		private static void PopulateCategoriesToMakiDictionary ()
		{
			foreach(var item in CategoriesToMakiDictionary)
			{
				foreach(string makiTag in item.Value)
				{
					if (!MakiToCategoriesDictionary.ContainsKey(makiTag))
					{
						MakiToCategoriesDictionary.Add(makiTag, item.Key);
					}
				}
			}
		}

		/// <summary>
		/// Gets the maki tags list from a <see cref="LocationPrefabCategories"/> category
		/// </summary>
		/// <returns>The list of maki tags from supplied category.</returns>
		/// <param name="category"><see cref="LocationPrefabCategories"/></param>
		public static List<string> GetMakiListFromCategory(LocationPrefabCategories category)
		{
			List<string> returnList = new List<string>();

			CategoriesToMakiDictionary.TryGetValue(category, out returnList);

			return returnList;
		}

		/// <summary>
		/// Gets the <see cref="LocationPrefabCategories"/> category that the maki tag belongs to.
		/// </summary>
		/// <returns>The <see cref="LocationPrefabCategories"/>category from maki tag.</returns>
		/// <param name="makiTag">Maki tag</param>
		public static LocationPrefabCategories GetCategoryFromMakiTag(string makiTag)
		{
			LocationPrefabCategories returnCategory;

			if (MakiToCategoriesDictionary.TryGetValue(makiTag, out returnCategory))
				return returnCategory;

			return LocationPrefabCategories.AnyCategory;
		}
	}
}
