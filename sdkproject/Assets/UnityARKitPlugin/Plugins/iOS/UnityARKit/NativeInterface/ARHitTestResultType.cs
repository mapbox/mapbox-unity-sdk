using System;

namespace UnityEngine.XR.iOS
{
	[Flags]
    public enum ARHitTestResultType : long
	{
		/** Result type from intersecting the nearest feature point. */
		ARHitTestResultTypeFeaturePoint     = (1 << 0),

		/** A real-world planar surface detected by the search (without a corresponding anchor), whose orientation is perpendicular to gravity. */
		ARHitTestResultTypeEstimatedHorizontalPlane  = (1 << 1),

		/** A real-world planar surface detected by the search, whose orientation is parallel to gravity. */
		ARHitTestResultTypeEstimatedVerticalPlane    = (1 << 2),

		/** Result type from intersecting with an existing plane anchor. */
        ARHitTestResultTypeExistingPlane    = (1 << 3),

        /** Result type from intersecting with an existing plane anchor, taking into account the plane's extent. */
        ARHitTestResultTypeExistingPlaneUsingExtent  = ( 1 << 4),

		/** A plane anchor already in the scene (detected with the planeDetection option), respecting the plane's estimated size and shape. **/
		ARHitTestResultTypeExistingPlaneUsingGeometry = (1 << 5)

	}
}

