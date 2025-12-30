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
using Microsoft.Extensions.Logging;

namespace DupTerminator.ImageHash
{
    internal class PHashService : IPHashService
    {
        private const int SIZE = 64;
        private const int DCT_SIZE = 8;
        private static readonly double _sqrt2DivSize = Math.Sqrt(2.0 / SIZE);
        private static readonly double _sqrt2 = Math.Sqrt(2.0);

        public PHashService(ILogger<PHashService> logger)
        {
            _logger = logger;
        }

        // SIMD-оптимизированные коэффициенты DCT
        private static readonly List<Vector<double>>[] _dctCoeffsSimd = GenerateDctCoeffsSimd();
        private readonly ILogger<PHashService> _logger;

        public (ulong? phash, int width, int height) CalculatePHash(string path)
        {
            using var image = LoadAndPreprocessImage(path, out int width, out int height);
            if (image != null)
            {
                var dctCoefficients = ComputeDCT(image);
                return (ComputeHash(dctCoefficients), width, height);
            }
            return (null, width, height);
        }

        public (ulong? phash, int width, int height) CalculatePHash(Stream stream)
        {
            using var image = LoadAndPreprocessImage(stream, out int width, out int height);
            if (image != null)
            {
                var dctCoefficients = ComputeDCT(image);
                return (ComputeHash(dctCoefficients), width, height);
            }
            return (null, width, height);
        }

        public bool IsSupportedExtension(string extension)
        {
            string ext = extension.ToLowerInvariant();

            if (ext == ".bmp" || ext == ".gif" || ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".tiff" || ext == ".exif")
                return true;
            return false;
        }

        private Bitmap LoadAndPreprocessImage(Stream stream, out int width, out int height)
        {
            try
            {
                using var original = new Bitmap(stream);
                width = original.Width;
                height = original.Height;

                return PreprocessImage(original);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                width = 0;
                height = 0;
                return null;
            }
        }


        private static Bitmap LoadAndPreprocessImage(string imagePath, out int width, out int height)
        {
            using var original = new Bitmap(imagePath);
            width = original.Width;
            height = original.Height;

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

            // Вычисление DCT по строкам
            for (int i = 0; i < SIZE; i++)
            {
                var row = new double[SIZE];
                for (int j = 0; j < SIZE; j++)
                    row[j] = pixels[i, j];

                Dct1D_SIMD(row, dctCoefficients, i);
            }

            // Вычисление DCT по столбцам
            for (int j = 0; j < SIZE; j++)
            {
                var column = new double[SIZE];
                for (int i = 0; i < SIZE; i++)
                    column[i] = dctCoefficients[i, j];

                Dct1D_SIMD(column, dctCoefficients, j, limit: 8);
            }

            return dctCoefficients;
        }

        /// <summary>
        /// One dimensional Discrete Cosine Transformation.
        /// </summary>
        /// <param name="valuesRaw">Should be an array of doubles of length 64.</param>
        /// <param name="coefficients">Coefficients.</param>
        /// <param name="ci">Coefficients index.</param>
        /// <param name="limit">Limit.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Dct1D_SIMD(double[] valuesRaw, double[,] coefficients, int ci, int limit = SIZE)
        {
            Debug.Assert(valuesRaw.Length == 64, "This DCT method works with 64 doubles.");

            var valuesList = new List<Vector<double>>();
            var stride = Vector<double>.Count;
            for (var i = 0; i < valuesRaw.Length; i += stride)
            {
                valuesList.Add(new Vector<double>(valuesRaw, i));
            }

            for (var coef = 0; coef < limit; coef++)
            {
                for (var i = 0; i < valuesList.Count; i++)
                {
                    coefficients[ci, coef] += Vector.Dot(valuesList[i], _dctCoeffsSimd[coef][i]);
                }

                coefficients[ci, coef] *= _sqrt2DivSize;
                if (coef == 0)
                {
                    coefficients[ci, coef] *= _sqrt2;
                }
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
}
