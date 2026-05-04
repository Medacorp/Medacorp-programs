using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

public static class MatrixDecomposer
{
    public struct DecomposedTransform
    {
        public Vector3 translation;
        public Quaternion leftRotation;
        public Vector3 scale;
        public Quaternion rightRotation;
    }

    public static DecomposedTransform Decompose(Matrix4x4 m)
    {
        DecomposedTransform result = new DecomposedTransform();

        // 1. Extract translation
        result.translation = new Vector3(m.m03, m.m13, m.m23);

        // 2. Extract 3x3 linear part
        var A = DenseMatrix.OfArray(new double[,]
        {
            { m.m00, m.m01, m.m02 },
            { m.m10, m.m11, m.m12 },
            { m.m20, m.m21, m.m22 }
        });

        // 3. SVD
        var svd = A.Svd(true);

        Matrix<double> U = svd.U;
        Matrix<double> S = DenseMatrix.CreateDiagonal(3, 3, i => svd.S[i]);
        Matrix<double> Vt = svd.VT;

        // 4. Convert U and V to Unity rotations
        result.leftRotation = MatrixToQuaternion(U);
        result.rightRotation = MatrixToQuaternion(Vt.Transpose());

        // 5. Scale from diagonal of S
        result.scale = new Vector3(
            (float)S[0,0],
            (float)S[1,1],
            (float)S[2,2]
        );

        return result;
    }

    static Quaternion MatrixToQuaternion(Matrix<double> m)
    {
        Matrix4x4 unityM = new Matrix4x4();

        unityM.m00 = (float)m[0,0];
        unityM.m01 = (float)m[0,1];
        unityM.m02 = (float)m[0,2];

        unityM.m10 = (float)m[1,0];
        unityM.m11 = (float)m[1,1];
        unityM.m12 = (float)m[1,2];

        unityM.m20 = (float)m[2,0];
        unityM.m21 = (float)m[2,1];
        unityM.m22 = (float)m[2,2];

        unityM.m33 = 1f;

        return unityM.rotation;
    }
}