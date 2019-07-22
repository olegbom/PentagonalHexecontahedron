using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace PentagonalHexecontahedron
{
    public class IrregularPentagon
    {
        public static readonly uint VerticesCount = 5;
        public static readonly uint IndicesCount = 5;

        public static VertexPositionColor[] CreateVertices()
        { 
            var result = new VertexPositionColor[5];
            for (int i = 0; i < 5; i++)
            {
                result[i].Color = RgbaFloat.Orange;
            }

            double phi = (1 + Math.Sqrt(5.0)) / 2;

            double k = Math.Sqrt(81 * phi - 15);
            double ksi = (Math.Pow(44 + 12 * phi * (9 + k), 1 / 3.0) +
                          Math.Pow(44 + 12 * phi * (9 - k), 1 / 3.0) - 4)/12.0;

            double a = (1 + 2 * ksi) / (2 * (1 - 2 * ksi * ksi));

            double r = Math.Sqrt((1 + ksi) / (1 - ksi)) / 2.0;

            double angleAa = Math.Acos(8 * ksi - ksi * ksi * ksi * ksi - 1);
            double angleBb = Math.Acos(-ksi);

            result[0].Position = new Vector2(0.5f, (float)r);

            var rotationMatrix = Matrix3x2.CreateRotation((float) (Math.PI + angleBb));
            var dvPoint2 = Vector2.Transform(new Vector2(1, 0), rotationMatrix);

            result[1].Position = result[0].Position + dvPoint2;

            var dvPoint3 = Vector2.Transform(new Vector2((float)a, 0), rotationMatrix);
            dvPoint3 = Vector2.Transform(dvPoint3, rotationMatrix);

            result[2].Position = result[1].Position + dvPoint3;
            result[2].Position = new Vector2( 0, result[2].Position.Y);

            result[3].Position = new Vector2(-result[1].Position.X, result[1].Position.Y);

            result[4].Position = new Vector2(-result[0].Position.X, result[0].Position.Y);

            return result;
        }

        public static ushort[] CreateIndices()
        {
            return new ushort[] {2, 3, 1, 4, 0};
        }

    }
}
