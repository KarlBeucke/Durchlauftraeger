namespace Durchlauftraeger;

public static class Werkzeuge
{
    public static double[,] Uebertragungsmatrix(double l, double ei)
    {
        double[,] matrix = { { 1, l,-l*l/(2*ei),-l*l*l/(6*ei)},
                             { 0, 1,-l/ei      ,-l*l/(2*ei)},
                             { 0, 0,    1      ,l},
                             { 0, 0,    0      ,1}};
        return matrix;
    }
    public static double[] VectorVectorAdd(double[] vec1, double[] vec2)
    {
        var result = new double[vec1.Length];
        for (var i = 0; i < vec1.Length; i++)
        {
            result[i] = vec1[i] + vec2[i];
        }

        return result;
    }
    public static double[] VectorVectorMinus(double[] vec1, double[] vec2)
    {
        var result = new double[vec1.Length];
        for (var i = 0; i < vec1.Length; i++)
        {
            result[i] = vec1[i] - vec2[i];
        }

        return result;
    }

    public static double[] MatrixVectorMultiply(double[,] matrix, double[] vector)
    {
        var result = new double[matrix.GetLength(0)];
        for (var i = 0; i < matrix.GetLength(0); i++)
        {
            result[i] = 0;
            for (var k = 0; k < vector.Length; k++)
                result[i] += matrix[i, k] * vector[k];
        }
        return result;
    }

    public static double[,] MatrixMatrixMultiply(double[,] mat1, double[,] mat2)
    {
        if ((mat1.GetLength(0) != mat2.GetLength(0)))
            throw new BerechnungAusnahme("Mult: ungültige Matrixdimensionen \n\t["
                                         + mat1.Length + "," + mat1.GetLength(1) + "]x[" + mat2.Length + "," +
                                         mat2.GetLength(1) + "]");
        var result = new double[mat1.GetLength(0), mat2.GetLength(1)];
        for (var row = 0; row < mat1.GetLength(0); row++)
        {
            for (var col = 0; col < mat2.GetLength(1); col++)
            {
                var sum = 0.0;
                for (var k = 0; k < mat1.GetLength(1); k++)
                    sum += mat1[row, k] * mat2[k, col];
                result[row, col] = sum;
            }
        }
        return result;
    }
    public static double[,] Matrix2By2Inverse(double[,] kk)
    {
        var kkInv = new double[2, 2];
        var nenner = kk[0, 0] * kk[1, 1] - kk[0, 1] * kk[1, 0];
        kkInv[0, 0] = kk[1, 1] / nenner;
        kkInv[0, 1] = -kk[0, 1] / nenner;
        kkInv[1, 0] = -kk[1, 0] / nenner;
        kkInv[1, 1] = kk[0, 0] / nenner;
        return kkInv;
    }
    public static double[,] SubMatrix(double[,] matrix, int index1, int index2)
    {
        var subMatrix = new double[2, 2];
        subMatrix[0, 0] = matrix[index1, 0];
        subMatrix[0, 1] = matrix[index1, 1];
        subMatrix[1, 0] = matrix[index2, 0];
        subMatrix[1, 1] = matrix[index2, 1];
        return subMatrix;
    }
    public static double[] SubVektor(double[] vektor, int index1, int index2)
    {
        var subVektor = new double[2];
        subVektor[0] = vektor[index1];
        subVektor[1] = vektor[index2];
        return subVektor;
    }
}