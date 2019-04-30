namespace Mapbox.Unity.Location
{
	/// <summary>
	/// Doesn't do any calculations. Just passes latest value through.
	/// </summary>
	public class AngleSmoothingNoOp : AngleSmoothingAbstractBase
	{
		public override double Calculate() { return _angles[0]; }
	}
}
