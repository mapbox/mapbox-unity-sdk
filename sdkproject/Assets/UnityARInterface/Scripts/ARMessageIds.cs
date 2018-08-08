using System;

namespace UnityARInterface
{
    public static class ARMessageIds
    {
        public static Guid fromEditor { get { return new Guid("527b48f9-efdb-4072-a714-3264af15f277"); } }
        public static Guid frame { get { return new Guid("a182a359-a01d-44e1-a1a9-a87503edca9d"); } }
        public static Guid addPlane { get { return new Guid("85cf30ba-4edc-4e89-b125-3225c837491c"); } }
        public static Guid updatePlane { get { return new Guid("938ec46d-b1fc-453a-9952-ba1e8da0d1c0"); } }
        public static Guid removePlane { get { return new Guid("e9872a5e-a11a-4e3c-b53f-2911e4771e02"); } }
        public static Guid screenCaptureY { get { return new Guid("ecddd97e-8f6d-4b51-827f-80260016b0f7"); } }
        public static Guid screenCaptureUV { get { return new Guid("78dac8cb-10ac-4ab3-8715-3a1dca1f7d3b"); } }
        public static Guid screenCaptureParams { get { return new Guid("d6bb91ad-926a-4f18-83b8-17e66db6d172"); } }
        public static Guid pointCloud { get { return new Guid("4d4e67b6-8244-41b3-a0df-dadfbf43f6dc"); } }
        public static Guid lightEstimate { get { return new Guid("eb705b2b-6e69-468e-986c-37721fbdfd7d"); } }

        public static class SubMessageIds
        {
            public static Guid startService { get { return new Guid("0b3e1cb1-d233-43ba-afd0-6c2890159b4b"); } }
            public static Guid stopService { get { return new Guid("8b8504e1-d673-4fb6-95b7-88a3a23d2ebc"); } }
            public static Guid enableVideo { get { return new Guid("f2d57d81-1b8e-4d54-8ce8-8bc42b97b5d2"); } }
            public static Guid backgroundRendering { get { return new Guid("b95af19e-39db-4bd2-acca-e353fd822689"); } }
        }
    }
}
