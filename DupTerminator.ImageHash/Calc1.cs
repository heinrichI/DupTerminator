using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.ImageHash
{
    public class Calc1
    {
        public static Span<byte> LoadAndResizeToGray64(string path)
        {
            // 1. Загружаем картинку
            using var src = new Bitmap(path);
            using var tmp = new Bitmap(64, 64, PixelFormat.Format24bppRgb);
            using var g = Graphics.FromImage(tmp);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(src, 0, 0, 64, 64);

            // 2. lock bits и конвертация в gray
            var rect = new Rectangle(0, 0, 64, 64);
            var bmpData = tmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            int stride = bmpData.Stride;
            var dst = new byte[64 * 64];
            unsafe
            {
                byte* scan0 = (byte*)bmpData.Scan0;
                for (int y = 0; y < 64; y++)
                {
                    byte* row = scan0 + y * stride;
                    for (int x = 0; x < 64; x++)
                    {
                        // BGR24
                        byte b = row[x * 3 + 0];
                        byte gCol = row[x * 3 + 1];
                        byte r = row[x * 3 + 2];
                        // Luma Rec. 601
                        dst[y * 64 + x] = (byte)((r * 299 + gCol * 587 + b * 114) / 1000);
                    }
                }
            }
            tmp.UnlockBits(bmpData);
            return dst;  // Span byte[4096]
        }

        const int N = 8;
        const int SIZE = 64;   // 8×8, но мы делаем DCT на 64 длине
        static readonly double _sqrt2 = Math.Sqrt(2);
        static readonly double _sqrt2DivSize = _sqrt2 / SIZE;

        // генерируем разовые коэффициенты на 64
        static readonly List<Vector<double>>[] _dctCoeffsSimd = GenerateDctCoeffsSimd();

        private static List<Vector<double>>[] GenerateDctCoeffsSimd()
        {
            var results = new List<Vector<double>>[SIZE];
            for (var coef = 0; coef < SIZE; coef++)
            {
                var singleResultRaw = new double[SIZE];
                for (var i = 0; i < SIZE; i++)
                {
                    singleResultRaw[i] = Math.Cos(((2.0 * i) + 1.0) * coef * Math.PI / (2.0 * SIZE));
                }

                var singleResultList = new List<Vector<double>>();
                var stride = Vector<double>.Count;
                Debug.Assert(SIZE % stride == 0, "Size must be a multiple of SIMD stride");
                for (var i = 0; i < SIZE; i += stride)
                {
                    var v = new Vector<double>(singleResultRaw, i);
                    singleResultList.Add(v);
                }

                results[coef] = singleResultList;
            }

            return results;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Dct1D_SIMD(ReadOnlySpan<double> src, Span<double> dest)
        {
            int stride = Vector<double>.Count;
            int vecCount = SIZE / stride;

            // 1) разбиваем вход на векторы
            var vList = new Vector<double>[vecCount];
            for (int i = 0; i < vecCount; i++)
                vList[i] = new Vector<double>(src.Slice(i * stride, stride));

            // 2) для каждого коэффициента u считаем DCT
            for (int u = 0; u < SIZE; u++)
            {
                // сумма поэлементных умножений
                Vector<double> accV = Vector<double>.Zero;
                var coeffList = _dctCoeffsSimd[u];
                for (int i = 0; i < vecCount; i++)
                {
                    // ПОЭЛЕМЕНТНОЕ УМНОЖЕНИЕ вектора-значений на вектора-коэффициентов
                    accV += vList[i] * coeffList[i];
                }

                // 3) «сжимаем» вектор accV в одно число
                double sum = 0;
                for (int j = 0; j < stride; j++)
                    sum += accV[j];

                // 4) нормировка
                sum *= _sqrt2DivSize;
                if (u == 0)
                    sum *= _sqrt2;

                dest[u] = sum;
            }
        }

        /// <summary>
        /// One dimensional Discrete Cosine Transformation.
        /// </summary>
        /// <param name="valuesRaw">Should be an array of doubles of length 64.</param>
        /// <param name="coefficients">Coefficients.</param>
        /// <param name="ci">Coefficients index.</param>
        /// <param name="limit">Limit.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static void Dct1D_SIMD(double[] valuesRaw, double[,] coefficients, int ci, int limit = SIZE)
        //{
        //    Debug.Assert(valuesRaw.Length == 64, "This DCT method works with 64 doubles.");

        //    var valuesList = new List<Vector<double>>();
        //    var stride = Vector<double>.Count;
        //    for (var i = 0; i < valuesRaw.Length; i += stride)
        //    {
        //        valuesList.Add(new Vector<double>(valuesRaw, i));
        //    }

        //    for (var coef = 0; coef < limit; coef++)
        //    {
        //        for (var i = 0; i < valuesList.Count; i++)
        //        {
        //            coefficients[ci, coef] += Vector.Dot(valuesList[i], _dctCoeffsSimd[coef][i]);
        //        }

        //        coefficients[ci, coef] *= _sqrt2DivSize;
        //        if (coef == 0)
        //        {
        //            coefficients[ci, coef] *= _sqrt2;
        //        }
        //    }
        //}

        public static double[,] Dct2D_SIMD(ReadOnlySpan<byte> gray64)
        {
            // 1. Конвертация в double[]
            var tmp = new double[SIZE * SIZE];
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = gray64[i];

            // 2. DCT по строкам → rowDst
            var rowDst = new double[SIZE * SIZE];
            for (int y = 0; y < SIZE; y++)
            {
                // входная строка
                var srcRow = tmp.AsSpan(y * SIZE, SIZE);
                // куда писать результат
                var destRow = rowDst.AsSpan(y * SIZE, SIZE);
                Dct1D_SIMD(srcRow, destRow);
            }

            // 3. DCT по столбцам → finalFlat
            var finalFlat = new double[SIZE * SIZE];
            var colSrc = new double[SIZE];
            var colDst = new double[SIZE];

            for (int x = 0; x < SIZE; x++)
            {
                // 3.1 Собираем столбец из результатов строковой DCT
                for (int y = 0; y < SIZE; y++)
                    colSrc[y] = rowDst[y * SIZE + x];

                // 3.2 Применяем 1D-DCT к этому столбцу
                Dct1D_SIMD(colSrc, colDst);

                // 3.3 Распаковываем обратно в плоский массив
                for (int y = 0; y < SIZE; y++)
                    finalFlat[y * SIZE + x] = colDst[y];
            }

            // 4. Перекладываем плоский массив в [SIZE, SIZE]
            var result = new double[SIZE, SIZE];
            for (int y = 0; y < SIZE; y++)
                for (int x = 0; x < SIZE; x++)
                    result[y, x] = finalFlat[y * SIZE + x];

            return result;
        }

        //static double[,] Dct2D_SIMD(Span<byte> gray64)
        //{
        //    // 1) приводим к double
        //    var tmp = new double[SIZE * SIZE];
        //    for (int i = 0; i < SIZE * SIZE; i++)
        //        tmp[i] = gray64[i];

        //    var dst = new double[SIZE * SIZE].AsSpan();
        //    // 2) DCT по строкам
        //    for (int y = 0; y < SIZE; y++)
        //        Dct1D_SIMD(tmp.AsSpan(y * SIZE, SIZE), dst, y);

        //    // 3) DCT по столбцам: транспонируем логику
        //    var final = new double[SIZE * SIZE];
        //    var colSrc = new double[SIZE];
        //    var colDst = new double[SIZE];
        //    for (int x = 0; x < SIZE; x++)
        //    {
        //        // собираем столбец
        //        for (int y = 0; y < SIZE; y++)
        //            colSrc[y] = dst[y * SIZE + x];

        //        Dct1D_SIMD(colSrc, colDst, 0);  // запишем в colDst

        //        for (int u = 0; u < SIZE; u++)
        //            final[u * SIZE + x] = colDst[u];
        //    }
        //    return final;
        //}

        public static ulong ComputeHash(double[,] dct2d)
        {
            // 8×8 блок
            Span<double> block = stackalloc double[N * N];
            int idx = 0;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                    block[idx++] = dct2d[y, x];

            // среднее, пропуская элемент 0
            double sum = 0;
            for (int i = 1; i < block.Length; i++)
                sum += block[i];
            double avg = sum / (block.Length - 1);

            // формируем 64-бит
            ulong hash = 0;
            for (int i = 0; i < block.Length; i++)
            {
                bool bit = (i == 0) ? false : block[i] > avg;
                if (bit) hash |= 1UL << i;
            }
            return hash;
        }

        //string file = @"C:\img\test.jpg";
        //var gray64 = LoadAndResizeToGray64(file);
        //var dct2d = Dct2D_SIMD(gray64);
        //ulong phash = ComputeHash(dct2d);
        //Console.WriteLine($"pHash: 0x{phash:X16}");
    }
}
