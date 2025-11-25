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
using DupTerminator.BusinessLogic.Abstraction;

/*
namespace DupTerminator.ImageHash
{
    internal class PHashServiceSeek : IPHashService
    {
        private const int SIZE = 64;
        private const int DCT_SIZE = 8;
        private static readonly double _sqrt2DivSize = Math.Sqrt(2.0 / SIZE);
        private static readonly double _sqrt2 = Math.Sqrt(2.0);

        // SIMD-оптимизированные коэффициенты DCT
        private static readonly Vector<double>[] _dctCoeffsSimd = GenerateDctCoeffsSimd();

        public (ulong phash, int width, int height) CalculatePHash(string path)
        {
            using var image = LoadAndPreprocessImage(path);
            var dctCoefficients = ComputeDCT(image);
            return ComputeHash(dctCoefficients);
        }

        public (ulong phash, int width, int height) CalculatePHash(Stream stream)
        {
            using var image = LoadAndPreprocessImage(stream);
            var dctCoefficients = ComputeDCT(image);
            return ComputeHash(dctCoefficients);
        }

        public bool IsSupportedExtension(string extension)
        {
            string ext = extension.ToLowerInvariant();

            if (ext == ".bmp" || ext == ".gif" || ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".tiff" || ext == ".exif")
                return true;
            return false;
        }

        private static Bitmap LoadAndPreprocessImage(Stream stream)
        {
            using var original = new Bitmap(stream);

            return PreprocessImage(original);
        }


        private static Bitmap LoadAndPreprocessImage(string imagePath)
        {
            using var original = new Bitmap(imagePath);

            return PreprocessImage(original);
        }

        private static Bitmap PreprocessImage(Bitmap original)
        {
            // Масштабирование до 64x64
            var scaled = new Bitmap(SIZE, SIZE);
            using (var graphics = Graphics.FromImage(scaled))
            {
                graphics.DrawImage(original, 0, 0, SIZE, SIZE);
            }

            // Конвертация в оттенки серого
            var grayScale = ConvertToGrayscale(scaled);
            return grayScale;
        }

        private static Bitmap ConvertToGrayscale(Bitmap image)
        {
            // 1️⃣ Create an 8‑bit indexed destination
            var result = new Bitmap(image.Width, image.Height, PixelFormat.Format8bppIndexed);

            // 2️⃣ Grab the palette and make sure it exists
            var palette = result.Palette;
            if (palette.Entries.Length == 0)
                throw new InvalidOperationException(
                    "The destination bitmap has no palette – did you use the correct PixelFormat?");

            // 3️⃣ Fill the palette with shades of gray
            for (int i = 0; i < palette.Entries.Length; i++)
            {
                palette.Entries[i] = Color.FromArgb(i, i, i);
            }

            // 4️⃣ Commit the palette back to the bitmap
            result.Palette = palette;

            var rect = new Rectangle(0, 0, image.Width, image.Height);
            var sourceData = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var destData = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            try
            {
                // SIMD-оптимизированное преобразование в градации серого
                ConvertToGrayscaleSimd(sourceData, destData);
            }
            finally
            {
                image.UnlockBits(sourceData);
                result.UnlockBits(destData);
            }

            return result;
        }

        private static unsafe void ConvertToGrayscaleSimd(BitmapData source, BitmapData dest)
        {
            var sourcePtr = (byte*)source.Scan0;
            var destPtr = (byte*)dest.Scan0;
            var width = source.Width;
            var height = source.Height;
            var sourceStride = source.Stride;
            var destStride = dest.Stride;

            // Предварительное вычисление коэффициентов для всех каналов
            var coefficients = new Vector<float>[3]
            {
                new Vector<float>(0.299f),
                new Vector<float>(0.587f),
                new Vector<float>(0.114f)
            };

            for (int y = 0; y < height; y++)
            {
                var sourceRow = sourcePtr + y * sourceStride;
                var destRow = destPtr + y * destStride;

                int x = 0;
                while (x < width)
                {
                    int elementsToProcess = Math.Min(Vector<float>.Count, width - x);

                    // Загрузка RGB компонентов
                    Vector<float> r = LoadRgbComponent(sourceRow, x, 0, elementsToProcess);
                    Vector<float> g = LoadRgbComponent(sourceRow, x, 1, elementsToProcess);
                    Vector<float> b = LoadRgbComponent(sourceRow, x, 2, elementsToProcess);

                    // Вычисление grayscale
                    Vector<float> gray = r * coefficients[0] + g * coefficients[1] + b * coefficients[2];

                    // Сохранение результата
                    StoreGrayResult(destRow, x, gray, elementsToProcess);

                    x += Vector<float>.Count;
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector<float> LoadRgbComponent(byte* sourceRow, int startX, int componentOffset, int count)
        {
            var result = new Vector<float>();
            ref float resultRef = ref Unsafe.As<Vector<float>, float>(ref result);

            for (int i = 0; i < count; i++)
            {
                int pixelOffset = (startX + i) * 3 + componentOffset;
                Unsafe.Add(ref resultRef, i) = sourceRow[pixelOffset];
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void StoreGrayResult(byte* destRow, int startX, Vector<float> gray, int count)
        {
            ref float grayRef = ref Unsafe.As<Vector<float>, float>(ref gray);

            for (int i = 0; i < count; i++)
            {
                float value = Unsafe.Add(ref grayRef, i);
                destRow[startX + i] = (byte)Math.Clamp(value, 0, 255);
            }
        }

        private static double[,] ComputeDCT(Bitmap image)
        {
            var pixels = GetPixelValues(image);
            var dctCoefficients = new double[SIZE, SIZE];

            // Temporary buffer for intermediate results
            var tempBuffer = new double[SIZE, SIZE];

            // First pass: DCT on rows
            for (int y = 0; y < SIZE; y++)
            {
                var row = new double[SIZE];
                for (int x = 0; x < SIZE; x++)
                    row[x] = pixels[y, x];

                Dct1DSimd(row, tempBuffer, y, false);
            }

            // Second pass: DCT on columns
            for (int x = 0; x < SIZE; x++)
            {
                var column = new double[SIZE];
                for (int y = 0; y < SIZE; y++)
                    column[y] = tempBuffer[y, x];

                Dct1DSimd(column, dctCoefficients, x, true);
            }

            return dctCoefficients;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Dct1DSimd(double[] values, double[,] coefficients, int index, bool isColumnTransform)
        {
            Debug.Assert(values.Length == SIZE);

            var stride = Vector<double>.Count;
            int vectorCount = SIZE / stride;

            for (int u = 0; u < SIZE; u++)
            {
                double sum = 0.0;

                for (int vectorIndex = 0; vectorIndex < vectorCount; vectorIndex++)
                {
                    int start = vectorIndex * stride;
                    var valueVector = new Vector<double>(values, start);
                    var cosVector = _dctCoeffsSimd[u * vectorCount + vectorIndex];

                    sum += Vector.Dot(valueVector, cosVector);
                }

                // Handle remaining elements if SIZE is not divisible by stride
                for (int i = vectorCount * stride; i < SIZE; i++)
                {
                    sum += values[i] * Math.Cos(((2.0 * i) + 1.0) * u * Math.PI / (2.0 * SIZE));
                }

                // Apply normalization
                double cu = (u == 0) ? 1.0 / Math.Sqrt(SIZE) : Math.Sqrt(2.0 / SIZE);
                double coefficientValue = cu * sum;

                if (isColumnTransform)
                    coefficients[index, u] = coefficientValue;
                else
                    coefficients[u, index] = coefficientValue;
            }
        }


        private static double[,] GetPixelValues(Bitmap image)
        {
            var values = new double[SIZE, SIZE];
            var rect = new Rectangle(0, 0, SIZE, SIZE);

            // Lock the bitmap for reading
            var data = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    // Calculate the number of bytes per pixel
                    int bytesPerPixel = Image.GetPixelFormatSize(data.PixelFormat) / 8;

                    // Get the pointer to the first pixel
                    byte* ptr = (byte*)data.Scan0;

                    for (int y = 0; y < SIZE; y++)
                    {
                        // Get the address of the first pixel in the row
                        byte* row = ptr + y * data.Stride;

                        for (int x = 0; x < SIZE; x++)
                        {
                            // Calculate the index of the current pixel
                            int pixelIndex = x * bytesPerPixel;

                            // Extract the ARGB components
                            byte b = row[pixelIndex];
                            byte g = row[pixelIndex + 1];
                            byte r = row[pixelIndex + 2];
                            // byte a = row[pixelIndex + 3]; // Alpha channel (not used)

                            // Apply BT.601 luminance formula
                            values[y, x] = 0.299 * r + 0.587 * g + 0.114 * b;
                        }
                    }
                }
            }
            finally
            {
                image.UnlockBits(data);
            }

            return values;
        }

        private static Vector<double>[] GenerateDctCoeffsSimd()
        {
            var stride = Vector<double>.Count;
            int vectorCount = SIZE / stride;
            var results = new Vector<double>[SIZE * vectorCount];

            for (int u = 0; u < SIZE; u++)
            {
                for (int vectorIndex = 0; vectorIndex < vectorCount; vectorIndex++)
                {
                    var coefficients = new double[stride];
                    for (int i = 0; i < stride; i++)
                    {
                        int x = vectorIndex * stride + i;
                        coefficients[i] = Math.Cos(((2.0 * x) + 1.0) * u * Math.PI / (2.0 * SIZE));
                    }
                    results[u * vectorCount + vectorIndex] = new Vector<double>(coefficients);
                }
            }

            return results;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ComputeAverageSimd(double[] values)
        {
            var sumVector = Vector<double>.Zero;
            var valuesSpan = new Span<double>(values);
            var stride = Vector<double>.Count;

            for (int i = 0; i < valuesSpan.Length - stride; i += stride)
            {
                var valueVector = new Vector<double>(valuesSpan.Slice(i, stride));
                sumVector += valueVector;
            }

            double sum = 0;
            for (int i = 0; i < Vector<double>.Count; i++)
            {
                sum += sumVector[i];
            }

            // Добавляем оставшиеся элементы
            for (int i = valuesSpan.Length - valuesSpan.Length % stride; i < valuesSpan.Length; i++)
            {
                sum += valuesSpan[i];
            }

            return sum / valuesSpan.Length;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CalculateMedian64Values(IReadOnlyCollection<double> values)
        {
            Debug.Assert(values.Count == 64, "This DCT method works with 64 doubles.");
            return values.OrderBy(value => value).Skip(31).Take(2).Average();
        }

        private static ulong ComputeHash(double[,] dctCoefficients)
        {
            // Берем только левый верхний блок 8x8 (игнорируем DC коэффициент)
            var significantCoefficients = new double[DCT_SIZE * DCT_SIZE - 1];
            int index = 0;

            for (int i = 0; i < DCT_SIZE; i++)
            {
                for (int j = 0; j < DCT_SIZE; j++)
                {
                    if (i == 0 && j == 0) continue; // Пропускаем DC коэффициент
                    significantCoefficients[index++] = dctCoefficients[i, j];
                }
            }

            // Вычисляем среднее значение с использованием SIMD
            var average = ComputeAverageSimd(significantCoefficients);

            // Строим хэш
            // Only use the top 8x8 values.
            var top8X8 = new double[SIZE];
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    top8X8[(y * 8) + x] = dctCoefficients[y, x];
                }
            }

            // Get Median.
            var median = CalculateMedian64Values(top8X8);

            // Calculate hash.
            var mask = 1UL << (SIZE - 1);
            var hash = 0UL;

            for (var i = 0; i < SIZE; i++)
            {
                if (top8X8[i] > median)
                {
                    hash |= mask;
                }

                mask >>= 1;
            }


            return hash;
        }
    }
}*/
