using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PentagonalHexecontahedron
{
    [StructLayout(LayoutKind.Sequential)]
    public struct InstanceInfo
    {
        public static uint Size { get; } = 20;

        public Vector3 SphericalCoordinates;
        public float Rotation;
        public float Scale;


        public InstanceInfo(Vector3 sphericalCoordinates, float rotation, float scale)
        {
            SphericalCoordinates = sphericalCoordinates;
            Rotation = rotation;
            Scale = scale;
           
        }
    }
}