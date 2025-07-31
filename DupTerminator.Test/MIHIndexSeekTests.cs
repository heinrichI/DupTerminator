using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.ImageHash;
using Xunit;

namespace DupTerminator.Test
{
    public class MIHIndexSeekTests : IDisposable
    {
        private readonly MIHIndexSeek _index;
        private const int DEFAULT_HASH_SIZE = 256;

        public MIHIndexSeekTests()
        {
            _index = new MIHIndexSeek(DEFAULT_HASH_SIZE);
        }

        public void Dispose()
        {
            _index.Dispose();
        }

        [Fact]
        public void Constructor_ValidHashSize_CreatesInstance()
        {
            // Arrange & Act
            using var index = new MIHIndexSeek(256);

            // Assert
            Assert.NotNull(index);
        }

        [Theory]
        [InlineData(255)]
        [InlineData(257)]
        [InlineData(7)]
        public void Constructor_InvalidHashSize_ThrowsArgumentException(int invalidSize)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new MIHIndexSeek(invalidSize));
        }

        [Fact]
        public void Update_ValidHash_AddsToItems()
        {
            // Arrange
            var validHash = new string('a', 64); // 256 bits = 64 hex chars

            // Act
            _index.Update(new[] { validHash }, "test");

            // Assert - Can't directly test internal state, but no exception thrown
        }

        [Fact]
        public void Update_InvalidHash_ThrowsArgumentException()
        {
            // Arrange
            var invalidHash = "invalid";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _index.Update(new[] { invalidHash }, "test"));
        }

        [Fact]
        public void Update_AfterTraining_ThrowsInvalidOperationException()
        {
            // Arrange
            var validHash = new string('a', 64);
            _index.Update(new[] { validHash }, "test");
            _index.Train();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _index.Update(new[] { validHash }, "test"));
        }

        [Fact]
        public void Update_MultipleCategories_SameHash_AddsBothCategories()
        {
            // Arrange
            var hash = new string('a', 64);

            // Act
            _index.Update(new[] { hash }, "cat1");
            _index.Update(new[] { hash }, "cat2");
            _index.Train();

            // Assert
            var results = _index.Query(hash).ToList();
            Assert.Single(results);
            Assert.Equal(2, results[0].Categories.Count);
            Assert.Contains("cat1", results[0].Categories);
            Assert.Contains("cat2", results[0].Categories);
        }

        [Fact]
        public void Train_WithoutItems_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _index.Train());
        }

        [Fact]
        public void Train_WithItems_SetsTrainedFlag()
        {
            // Arrange
            var hash = new string('a', 64);
            _index.Update(new[] { hash }, "test");

            // Act
            _index.Train();

            // Assert - Can't directly test _trained, but Query should work
            var results = _index.Query(hash).ToList();
            Assert.Single(results);
        }

        [Fact]
        public void Query_BeforeTraining_ThrowsInvalidOperationException()
        {
            // Arrange
            var hash = new string('a', 64);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _index.Query(hash));
        }

        [Fact]
        public void Query_InvalidHash_ThrowsArgumentException()
        {
            // Arrange
            var validHash = new string('a', 64);
            _index.Update(new[] { validHash }, "test");
            _index.Train();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _index.Query("invalid"));
        }

        [Fact]
        public void Query_ExactMatch_ReturnsResultWithZeroDistance()
        {
            // Arrange
            var hash = new string('a', 64);
            _index.Update(new[] { hash }, "test");
            _index.Train();

            // Act
            var results = _index.Query(hash).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal(hash, results[0].Hash);
            Assert.Equal("test", results[0].Categories.Single());
            Assert.Equal(0, results[0].HammingDistance);
        }

        [Fact]
        public void Query_SimilarHashWithinThreshold_ReturnsResult()
        {
            // Arrange - Create two similar hashes (differ by one bit)
            var hash1 = new string('a', 64);
            var hash2 = new string('a', 63) + 'b'; // One character difference

            _index.Update(new[] { hash1 }, "test");
            _index.Train();

            // Act
            var results = _index.Query(hash2).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal(1, results[0].HammingDistance);
        }

        [Fact]
        public void Query_HammingDistance7()
        {
            // Arrange - Create two similar hashes (differ by one bit)
            var hash1 = new string('a', 64);
            var hash2 = new string('a', 57) + "bbbbbbb"; // One character difference

            _index.Update(new[] { hash1 }, "test");
            _index.Train();

            // Act
            var results = _index.Query(hash2).ToList();

            // Assert
            Assert.Single(results);
            Assert.Equal(7, results[0].HammingDistance);
        }

        [Fact]
        public void Query_HammingDistance7_ManyRandom()
        {
            // Arrange - Create two similar hashes (differ by one bit)
            var hash1 = new string('a', 64);
            var hash2 = new string('a', 57) + "bbbbbbb"; // One character difference

            List<string> hashes = new List<string>();
            hashes.Add(hash1);
            for (int i = 0; i < 10000; i++)
            {
                hashes.Add(GenerateRandomHexString());
            }
            _index.Update(hashes, "test");
            _index.Train();

            // Act
            var results = _index.Query(hash2).ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.Equal(7, results[0].HammingDistance);
        }

        /// <summary>
        /// Returns a cryptographically‑strong random string of the given length.
        /// Each character is in the set [0‑9a‑f].
        /// The default is 64 characters, which equals 32 random bytes.
        /// </summary>
        /// <param name="length">The length of the resulting string. Must be even.</param>
        /// <returns>A random hex string.</returns>
        public static string GenerateRandomHexString(int length = 64)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive.");

            // If length is odd we still want a string of that length,
            // so just produce floor(length/2) bytes + an extra half byte.
            // For simplicity we disallow odd lengths in this example.
            if (length % 2 != 0)
                throw new ArgumentException("Length must be even to avoid partial hex digits.", nameof(length));

            // One byte produces two hex digits, so we need length/2 bytes.
            int byteCount = length / 2;
            byte[] data = new byte[byteCount];

            // Cryptographically strong random filler (Windows: CNG; Linux/macOS: /dev/urandom).
            RandomNumberGenerator.Fill(data);

            // Convert each byte to its two‑character lowercase hex representation.
            // e.g., 0xAF → \"af\"
            //  Array is small (32 bytes for 64 chars), so LINQ/.Select is fine.
            return string.Concat(data.Select(b => b.ToString("x2")));
        }

        [Fact]
        public void Query_HashOutsideThreshold_ReturnsEmpty()
        {
            // Arrange - Create very different hashes
            var hash1 = new string('a', 64);
            var hash2 = new string('b', 64);

            _index.Update(new[] { hash1 }, "test");
            _index.Train();

            // Act
            var results = _index.Query(hash2).ToList();

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void Query_MultipleHashes_ReturnsAllMatches()
        {
            // Arrange
            var hash1 = new string('a', 64);
            var hash2 = new string('a', 63) + 'b';
            var hash3 = new string('a', 63) + 'c';

            _index.Update(new[] { hash1, hash2 }, "test");
            _index.Train();

            // Act
            var results = _index.Query(hash3).ToList();

            // Assert - Should find both similar hashes
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void Dispose_ClearsResources()
        {
            // Arrange
            var hash = new string('a', 64);
            _index.Update(new[] { hash }, "test");
            _index.Train();

            // Act
            _index.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => _index.Query(hash));
        }

        [Fact]
        public void Dispose_MultipleCalls_DoesNotThrow()
        {
            // Act & Assert
            _index.Dispose();
            _index.Dispose(); // Should not throw
        }

        [Theory]
        [InlineData(16, 32)] // Default values
        [InlineData(8, 16)]
        [InlineData(32, 64)]
        public void Train_WithCustomParameters_WorksCorrectly(int wordLength, int threshold)
        {
            // Arrange
            var hash = new string('a', 64);
            _index.Update(new[] { hash }, "test");

            // Act
            _index.Train(wordLength, threshold);

            // Assert
            var results = _index.Query(hash).ToList();
            Assert.Single(results);
        }

        //[Fact]
        //public void ExtractWordHex_ExtractsCorrectBits()
        //{
        //    // This is a white-box test for the private method
        //    // We'll create a test byte array and verify extraction

        //    // Arrange - Create a byte array with specific pattern
        //    byte[] testBytes = { 0xFF, 0x00, 0xAA, 0x55 };
        //    var hashSpan = new ReadOnlySpan<byte>(testBytes);

        //    // Act - Extract first 16 bits (should be 0xFF00)
        //    string result = ExtractWordHexPrivate(_index, hashSpan, 0, 16);

        //    // Assert
        //    Assert.Equal("ff00", result);
        //}

        // Helper method to access private ExtractWordHex via reflection
        //private static string ExtractWordHexPrivate(MIHIndexSeek index, ReadOnlySpan<byte> hash, int startBit, int length)
        //{
        //    var method = typeof(MIHIndexSeek).GetMethod("ExtractWordHex",
        //        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        //    return (string)method.Invoke(index, new object[] { hash, startBit, length });
        //}

        [Fact]
        public void GetHammingDistanceSIMD_IdenticalHashes_ReturnsZero()
        {
            // Arrange
            byte[] hash1 = new byte[32]; // 256 bits
            byte[] hash2 = new byte[32];

            // Act - Use reflection to call private method
            int distance = GetHammingDistanceSIMDPrivate(_index, hash1, hash2, 32);

            // Assert
            Assert.Equal(0, distance);
        }

        [Fact]
        public void GetHammingDistanceSIMD_DifferentHashes_ReturnsCorrectDistance()
        {
            // Arrange
            byte[] hash1 = new byte[32];
            byte[] hash2 = new byte[32];
            hash2[0] = 0xFF; // 8 bits different

            // Act
            int distance = GetHammingDistanceSIMDPrivate(_index, hash1, hash2, 100);

            // Assert
            Assert.Equal(8, distance);
        }

        // Helper method to access private GetHammingDistanceSIMD via reflection
        private static int GetHammingDistanceSIMDPrivate(MIHIndexSeek index, byte[] hash1, byte[] hash2, int maxDistance)
        {
            var method = typeof(MIHIndexSeek).GetMethod("GetHammingDistanceSIMD",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return (int)method.Invoke(index, new object[] { hash1, hash2, maxDistance });
        }

        [Fact]
        public void HexStringToByteArray_RoundTrip_WorksCorrectly()
        {
            // Arrange
            string originalHex = "aabbccddeeff00112233445566778899";

            // Act
            byte[] bytes = HexStringToByteArrayPrivate(_index, originalHex);
            string roundTrip = ByteArrayToHexStringPrivate(_index, bytes);

            // Assert
            Assert.Equal(originalHex, roundTrip);
        }

        // Helper methods for private conversion methods
        private static byte[] HexStringToByteArrayPrivate(MIHIndexSeek index, string hex)
        {
            var method = typeof(MIHIndexSeek).GetMethod("HexStringToByteArray",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            return (byte[])method.Invoke(null, new object[] { hex });
        }

        private static string ByteArrayToHexStringPrivate(MIHIndexSeek index, byte[] bytes)
        {
            var method = typeof(MIHIndexSeek).GetMethod("ByteArrayToHexString",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            return (string)method.Invoke(null, new object[] { bytes });
        }
    }
}
