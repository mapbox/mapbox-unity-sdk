using System;

namespace UnityEngine.XR.iOS
{
	public class UnityARMatrixOps
	{

		public static Matrix4x4 UnityToARKitCoordChange(Vector3 position, Quaternion rotation)
		{
			Matrix4x4 result = new Matrix4x4 ();
			//do the conversions back to ARKit space
			result.SetTRS (new Vector3 (position.x, position.y, -position.z),
				new Quaternion (rotation.x, rotation.y, -rotation.z, -rotation.w),
				Vector3.one);
			return result;

		}

		public static Matrix4x4 GetMatrix(UnityARMatrix4x4 unityMatrix)
		{
			var matrix = new Matrix4x4();
			matrix.SetColumn(0, unityMatrix.column0);
			matrix.SetColumn(1, unityMatrix.column1);
			matrix.SetColumn(2, unityMatrix.column2);
			matrix.SetColumn(3, unityMatrix.column3);

			return matrix;
		}

		public static UnityARMatrix4x4 GetMatrix(Matrix4x4 nativeMatrix)
		{
			var matrix = new UnityARMatrix4x4();
			matrix.column0 = nativeMatrix.GetColumn(0);
			matrix.column1 = nativeMatrix.GetColumn(1);
			matrix.column2 = nativeMatrix.GetColumn(2);
			matrix.column3 = nativeMatrix.GetColumn(3);

			return matrix;
		}

		public static Pose GetPose(UnityARMatrix4x4 unityMatrix)
		{
			return GetPose(GetMatrix(unityMatrix));
		}

		public static Pose GetPose(Matrix4x4 matrix)
		{
			return new Pose(GetPosition(matrix), GetRotation(matrix));
		}

		public static Vector3 GetPosition(Matrix4x4 matrix)
		{
			return GetPosition(matrix.GetColumn(3));
		}

		public static Quaternion GetRotation(Matrix4x4 matrix)
		{
			// Convert from ARKit's right-handed coordinate
			// system to Unity's left-handed
			Quaternion rotation = QuaternionFromMatrix(matrix);
			rotation.z = -rotation.z;
			rotation.w = -rotation.w;

			return rotation;
		}

		public static Vector3 GetPosition(Vector3 position)
		{
			// Convert from ARKit's right-handed coordinate
			// system to Unity's left-handed
			position.z = -position.z;
			return position;
		}

		static Quaternion QuaternionFromMatrix(Matrix4x4 m) {
			// Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
			Quaternion q = new Quaternion();
			q.w = Mathf.Sqrt( Mathf.Max( 0, 1 + m[0,0] + m[1,1] + m[2,2] ) ) / 2; 
			q.x = Mathf.Sqrt( Mathf.Max( 0, 1 + m[0,0] - m[1,1] - m[2,2] ) ) / 2; 
			q.y = Mathf.Sqrt( Mathf.Max( 0, 1 - m[0,0] + m[1,1] - m[2,2] ) ) / 2; 
			q.z = Mathf.Sqrt( Mathf.Max( 0, 1 - m[0,0] - m[1,1] + m[2,2] ) ) / 2; 
			q.x *= Mathf.Sign( q.x * ( m[2,1] - m[1,2] ) );
			q.y *= Mathf.Sign( q.y * ( m[0,2] - m[2,0] ) );
			q.z *= Mathf.Sign( q.z * ( m[1,0] - m[0,1] ) );
			return q;
		}

	}
}

