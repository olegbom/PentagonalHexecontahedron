using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Veldrid;

namespace PentagonalHexecontahedron
{
    public class IrregularPentagon
    {
        #region Static Members
        public static readonly uint VerticesCount = 5;
        public static readonly uint IndicesCount = 5;

        public static readonly VertexPositionColor[] Vertices;

        public static readonly ushort[] Indices = {2, 3, 1, 4, 0};
        
        private static readonly double A;

        private static readonly double R;
        private static readonly double AngleAa;
        private static readonly double AngleBb;

        static IrregularPentagon()
        {
            double phi = (1 + Math.Sqrt(5.0)) / 2;

            double ksi = (Math.Pow(44 + 12 * phi * (9 + Math.Sqrt(81 * phi - 15)), 1 / 3.0) +
                          Math.Pow(44 + 12 * phi * (9 - Math.Sqrt(81 * phi - 15)), 1 / 3.0) - 4) / 12.0;

            A = (1 + 2 * ksi) / (2 * (1 - 2 * ksi * ksi));

            R = Math.Sqrt((1 + ksi) / (1 - ksi)) / 2.0;
            AngleAa = Math.Acos(8 * ksi * ksi - ksi * ksi * ksi * ksi - 1);
            AngleBb = Math.Acos(-ksi);

            Vertices = CreateVertices();
        }


        private static VertexPositionColor[] CreateVertices()
        { 
            var result = new VertexPositionColor[5];
            for (int i = 0; i < 5; i++)
            {
                result[i].Color = RgbaFloat.Orange;
            }

        
            result[0].Position = new Vector2(0.5f, (float)R);

            var rotationMatrix = Matrix3x2.CreateRotation((float) (Math.PI + AngleBb));
            var dvPoint2 = Vector2.Transform(new Vector2(1, 0), rotationMatrix);

            result[1].Position = result[0].Position + dvPoint2;

            var dvPoint3 = Vector2.Transform(new Vector2((float)A, 0), rotationMatrix);
            dvPoint3 = Vector2.Transform(dvPoint3, rotationMatrix);

            result[2].Position = result[1].Position + dvPoint3;
            result[2].Position = new Vector2( 0, result[2].Position.Y);

            result[3].Position = new Vector2(-result[1].Position.X, result[1].Position.Y);

            result[4].Position = new Vector2(-result[0].Position.X, result[0].Position.Y);

            return result;
        }

        private static ushort[] CreateIndices()
        {
            return new ushort[] {2, 3, 1, 4, 0};
        }
        #endregion

        public Matrix4x4 ModelMatrix { get; private set; } = Matrix4x4.Identity;
        public readonly IrregularPentagon[] Neighbors = new IrregularPentagon[5];

        public IrregularPentagon()
        {

        }

        public IrregularPentagon(IrregularPentagon parent, int number)
        {
            if (number < 0 || number > 4)
                throw new ArgumentException($"Номер соседа должен быть в диапазоне [0;4], не {number}");

            ModelMatrix = Matrix4x4.CreateTranslation(0, (float)R*2, 0) * 
                          Matrix4x4.CreateRotationZ((float) AngleBb) * 
                          parent.ModelMatrix;
        }
    }
}
