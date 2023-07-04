using System;

namespace Durchlauftraeger;

public class Gleichungslöser
{
    private readonly double[,] _matrix;                      //coefficient matrix, will be changed during solution
    private readonly double[] _vector;                       //coefficient vector (includes solution at the end)
    private readonly int _dimension;                         //dimension of quadratic matrix

    public Gleichungslöser(double[,] matrix, double[] vector)
    {
        _matrix = matrix;
        _vector = vector;
        _dimension = vector.Length;
    }

    public double[] MatrixVectorMultiply(double[,] matrix, double[] vector)
    {
        var result = new double[vector.Length];
        for (var i = 0; i < vector.Length; i++)
        {
            result[i] = 0;
            for (var k = 0; k < vector.Length; k++)
                result[i] += matrix[i, k] * vector[k];
        }
        return result;
    }

    public bool Decompose()
    {    //triangularization of coefficient matrix A = L * R
        int s;

        // evaluate elements in row "s"
        for (s = 1; s < _dimension; s++)
        {
            double sum;
            int k;
            int m;
            for (m = 0; m < s; m++)
            {
                sum = _matrix[s, m];
                for (k = 0; k < m; k++) sum -= _matrix[s, k] * _matrix[k, m];
                _matrix[s, m] = sum / _matrix[m, m];
            }

            // evaluate elements in column "s"
            int i;
            for (i = 0; i <= s; i++)
            {
                sum = _matrix[i, s];
                for (k = 0; k < i; k++) sum -= _matrix[i, k] * _matrix[k, s];
                _matrix[i, s] = sum;
            }

            // check diagonal element in row "s"
            if (!(Math.Abs(_matrix[s, s]) < double.Epsilon)) continue;
            throw new BerechnungAusnahme("Pivot in Zeile " + s + " kleiner als Fehlerschranke");
        }
        return true;
    }

    // solve system of linear equations
    public double[] Solve()
    {
        int s, k;

        // forward solution
        for (s = 0; s < _dimension; s++)
        {
            for (k = 0; k < s; k++) _vector[s] -= _matrix[s, k] * _vector[k];
        }

        // backward solution
        for (s = _dimension - 1; s >= 0; s--)
        {
            for (k = s + 1; k < _dimension; k++) _vector[s] -= _matrix[s, k] * _vector[k];
            _vector[s] /= _matrix[s, s];
        }
        return _vector;
    }
}