//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.Linq;
//using System.Numerics;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace DupTerminator.ImageHash
//{
//    public static class ImageHash
//    {
//        private const int TargetSize = 64;
//        private const int DctBlockSize = 8;
//        private const int FullBlockSize = TargetSize * TargetSize;
//        private const int DctCoeffsCount = DctBlockSize * DctBlockSize;

//        private static readonly float _sqrt2 = (float)Math.Sqrt(2.0);
//        private static readonly float _sqrt2DivSize = _sqrt2 / TargetSize;
//        private static readonly Vector<float>[] _dctCoeffsSimd = GenerateDctCoeffsSimd();

//        private static Vector<float>[] GenerateDctCoeffsSimd()
//        {
//            var results = new Vector<float>[TargetSize];
//            var stride = Vector<float>.Count;

//            for (int coef = 0; coef < TargetSize; coef++)
//            {
//                Span<float> coeffs = stackalloc float[TargetSize];
//                for (int i = 0; i < TargetSize; i++)
//                {
//                    coeffs[i] = (float)Math.Cos(((2.0 * i) + 1.0) * coef * Math.PI / (2.0 * TargetSize));
//                }

//                Span<float> padded = stackalloc float[TargetSize + stride];
//                coeffs.CopyTo(padded);
//                MemoryMarshal.AsBytes(padded).Slice(TargetSize * sizeof(float)).Fill(0);

//                results[coef] = MemoryMarshal.Cast<float, Vector<float>>(padded)[0];
//            }

//            return results;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
//        public static ulong CalculatePHash(ReadOnlySpan<byte> imageData, int width, int height, int stride)
//        {
//            // Этап 1: Масштабирование до 64x64 с использованием Span
//            Span<byte> scaled = stackalloc byte[FullBlockSize];
//            ResizeToTarget(imageData, width, height, stride, scaled);

//            // Этап 2: Преобразование в оттенки серого
//            Span<float> gray = stackalloc float[FullBlockSize];
//            ConvertToGrayscale(scaled, gray);

//            // Этап 3: Применение DCT
//            Span<float> dctResult = stackalloc float[FullBlockSize];
//            ApplyDct2D(gray, dctResult);

//            // Этап 4: Извлечение 8x8 блока
//            Span<float> dctBlock = stackalloc float[DctCoeffsCount];
//            ExtractDctBlock(dctResult, dctBlock);

//            // Этап 5: Вычисление хеша
//            return ComputeHash(dctBlock);
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static void ResizeToTarget(ReadOnlySpan<byte> src, int width, int height, int srcStride, Span<byte> dst)
//        {
//            float xRatio = width / (float)TargetSize;
//            float yRatio = height / (float)TargetSize;
//            var vectorSize = Vector<float>.Count;
//            int floatStride = vectorSize * 3;

//            for (int y = 0; y < TargetSize; y++)
//            {
//                float srcY = y * yRatio;
//                int iy = (int)srcY;
//                float yDelta = srcY - iy;

//                for (int x = 0; x < TargetSize; x++)
//                {
//                    float srcX = x * xRatio;
//                    int ix = (int)srcX;
//                    float xDelta = srcX - ix;

//                    float b = 0, g = 0, r = 0;
//                    for (int dy = 0; dy < 2; dy++)
//                    {
//                        int yOff = Math.Min(iy + dy, height - 1);
//                        for (int dx = 0; dx < 2; dx++)
//                        {
//                            int xOff = Math.Min(ix + dx, width - 1);
//                            int srcOffset = yOff * srcStride + xOff * 3;

//                            float w = (1 - Math.Abs(dx - xDelta)) * (1 - Math.Abs(dy - yDelta));
//                            b += w * src[srcOffset];
//                            g += w * src[srcOffset + 1];
//                            r += w * src[srcOffset + 2];
//                        }
//                    }

//                    dst[y * TargetSize + x] = (byte)(0.114 * b + 0.587 * g + 0.299 * r);
//                }
//            }
//        }

//        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
//        private static void ConvertToGrayscale(ReadOnlySpan<byte> src, Span<float> dst)
//        {
//            int chunks = src.Length / (Vector<float>.Count * 3);
//            int remainder = src.Length % (Vector<float>.Count * 3);

//            var bWeights = new Vector<float>(0.114f);
//            var gWeights = new Vector<float>(0.587f);
//            var rWeights = new Vector<float>(0.299f);

//            ref byte srcRef = ref MemoryMarshal.GetReference(src);
//            ref float dstRef = ref MemoryMarshal.GetReference(dst);

//            for (int i = 0; i < chunks; i++)
//            {
//                var b = Unsafe.ReadUnaligned<Vector<float>>(ref srcRef);
//                var g = Unsafe.ReadUnaligned<Vector<float>>(ref Unsafe.Add(ref srcRef, Vector<float>.Count));
//                var r = Unsafe.ReadUnaligned<Vector<float>>(ref Unsafe.Add(ref srcRef, Vector<float>.Count * 2));

//                Unsafe.WriteUnaligned(ref dstRef, b * bWeights + g * gWeights + r * rWeights);

//                srcRef = ref Unsafe.Add(ref srcRef, Vector<float>.Count * 3);
//                dstRef = ref Unsafe.Add(ref dstRef, Vector<float>.Count);
//            }

//            // Обработка остатка
//            for (int i = chunks * Vector<float>.Count * 3; i < src.Length; i += 3)
//            {
//                dst[i / 3] = 0.114f * src[i] + 0.587f * src[i + 1] + 0.299f * src[i + 2];
//            }
//        }

//        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
//        private static void ApplyDct2D(ReadOnlySpan<float> input, Span<float> output)
//        {
//            Span<float> temp = stackalloc float[FullBlockSize];

//            // Горизонтальное DCT
//            for (int i = 0; i < TargetSize; i++)
//            {
//                Dct1D(input.Slice(i * TargetSize, TargetSize),
//                     temp.Slice(i * TargetSize, TargetSize));
//            }

//            // Вертикальное DCT
//            for (int i = 0; i < TargetSize; i++)
//            {
//                Span<float> colTemp = stackalloc float[TargetSize];
//                for (int j = 0; j < TargetSize; j++)
//                {
//                    colTemp[j] = temp[j * TargetSize + i];
//                }

//                Dct1D(colTemp, output.Slice(i, FullBlockSize));
//            }
//        }

//        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
//        private static void Dct1D(ReadOnlySpan<float> src, Span<float> dst)
//        {
//            var vectorSize = Vector<float>.Count;
//            var scratch = stackalloc float[TargetSize + vectorSize];

//            for (int u = 0; u < TargetSize; u++)
//            {
//                float sum = 0;

//                // SIMD обработка
//                for (int i = 0; i < TargetSize; i += vectorSize)
//                {
//                    var vecSrc = Vector.LoadUnsafe(ref MemoryMarshal.GetReference(src) + i);
//                    var vecCoeffs = Vector.LoadUnsafe(ref MemoryMarshal.GetReference(_dctCoeffsSimd[u]) + i);
//                    sum += Vector.Dot(vecSrc, vecCoeffs);
//                }

//                // Нормализация
//                sum *= u == 0 ? _sqrt2DivSize * 0.5f : _sqrt2DivSize;
//                dst[u] = sum;
//            }
//        }

//        private static void ExtractDctBlock(ReadOnlySpan<float> src, Span<float> dst)
//        {
//            for (int i = 0; i < DctBlockSize; i++)
//            {
//                src.Slice(i * TargetSize, DctBlockSize).CopyTo(dst.Slice(i * DctBlockSize, DctBlockSize));
//            }
//        }

//        private static ulong ComputeHash(ReadOnlySpan<float> dctBlock)
//        {
//            // Вычисление среднего без DC-коэффициента
//            float sum = 0;
//            for (int i = 1; i < DctCoeffsCount; i++)
//            {
//                sum += dctBlock[i];
//            }
//            float mean = sum / (DctCoeffsCount - 1);

//            // Построение хеша
//            ulong hash = 0;
//            for (int i = 0; i < DctCoeffsCount; i++)
//            {
//                if (dctBlock[i] > mean)
//                {
//                    hash |= 1UL << (63 - i);
//                }
//            }
//            return hash;
//        }

//        // Загрузка изображения (пример использования)
//        public static ulong CreatePHashFromFile(string filePath)
//        {
//            using var bmp = new Bitmap(filePath);
//            var bmpData = bmp.LockBits(
//                new Rectangle(0, 0, bmp.Width, bmp.Height),
//                ImageLockMode.ReadOnly,
//                PixelFormat.Format24bppRgb
//            );

//            try
//            {
//                return CalculatePHash(
//                    new ReadOnlySpan<byte>(
//                        (void*)bmpData.Scan0,
//                        bmpData.Height * bmpData.Stride
//                    ),
//                    bmp.Width,
//                    bmp.Height,
//                    bmpData.Stride
//                );
//            }
//            finally
//            {
//                bmp.UnlockBits(bmpData);
//            }
//        }
//    }
//}
