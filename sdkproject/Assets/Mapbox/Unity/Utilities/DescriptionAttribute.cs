namespace Mapbox.VectorTile.Geometry
{


	using System;


#if ENABLE_WINMD_SUPPORT
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class DescriptionAttribute : Attribute
	{
		private readonly string description;
		public string Description { get { return description; } }
		public DescriptionAttribute(string description)
		{
			this.description = description;
		}
	}
#endif


}
