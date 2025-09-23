using System;
using System.Globalization;

namespace Mapbox.BaseModule.Data.Vector2d
{
    [Serializable]
    public struct LatitudeLongitude
    {
        public static LatitudeLongitude Invalid => new(MAX_LATITUDE * 2, MAX_LONGITUDE * 2);
        public static bool operator ==(in LatitudeLongitude a, in LatitudeLongitude b) => a.Equals(in b);
        public static bool operator !=(in LatitudeLongitude a, in LatitudeLongitude b) => !a.Equals(in b);
		
		
        public double Latitude;
        public double Longitude;

        public const double MAX_LATITUDE = 90;
        public const double MAX_LONGITUDE = 90;

        public LatitudeLongitude(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
        
        public override string ToString()
        {
            return string.Format(NumberFormatInfo.InvariantInfo, "{0},{1}", this.Latitude, this.Longitude);
        }

        public string ToStringLonLat()
        {
            return string.Format(NumberFormatInfo.InvariantInfo, "{0:F5},{1:F5}", this.Longitude, this.Latitude);
        }
        
        public static bool AlmostEqual(double a, double b, double tolerance = double.Epsilon)
        {
            return Math.Abs(a - b) < tolerance;
        }

        public readonly bool Equals(in LatitudeLongitude other)
        {
            return AlmostEqual(this.Latitude, other.Latitude) && AlmostEqual(this.Longitude, other.Longitude);
        }

        public override bool Equals(object obj) => obj is LatitudeLongitude other && Equals(in other);
        
        public bool IsValid()// technically invalid only if latitude is outside of [-90, 90] as longitude can wrap around
        {
            return Latitude >= -MAX_LATITUDE &&
                   Latitude <= MAX_LATITUDE &&
                   Longitude >= -MAX_LONGITUDE &&
                   Longitude <= MAX_LONGITUDE;
        }
        public override int GetHashCode()
        {
            return this.Latitude.GetHashCode() ^ this.Longitude.GetHashCode() << 2;
        }
    }
}