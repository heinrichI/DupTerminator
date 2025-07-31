using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.ImageHash
{
    /*
    /// <summary>
    /// Extension methods for IImageHash.
    /// </summary>
    public static class ImageHashExtensions
    {
        /// <summary>Calculate the hash of the image (stream) using the hashImplementation.</summary>
        /// <param name="hashImplementation">HashImplementation to calculate the hash.</param>
        /// <param name="stream">Stream should 'contain' raw image data.</param>
        /// <returns>hash value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="hashImplementation"/> or <paramref name="stream"/> is <c>null</c>.</exception>
        /// <exception cref="SixLabors.ImageSharp.UnknownImageFormatException">Thrown when stream content cannot be loaded as an image.</exception>
        public static ulong Hash(this IImageHash hashImplementation, Stream stream)
        {
            if (hashImplementation == null)
            {
                throw new ArgumentNullException(nameof(hashImplementation));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var image = Image.Load<Rgba32>(stream);
            return hashImplementation.Hash(image);
        }

        /// <summary>Asynchronously calculate the hash of the image (stream) using the hashImplementation.</summary>
        /// <param name="hashImplementation">HashImplementation to calculate the hash.</param>
        /// <param name="stream">Stream should 'contain' raw image data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>hash value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="hashImplementation"/> or <paramref name="stream"/> is <c>null</c>.</exception>
        /// <exception cref="SixLabors.ImageSharp.UnknownImageFormatException">Thrown when stream content cannot be loaded as an image.</exception>
        public static async Task<ulong> HashAsync(this IImageHash hashImplementation, Stream stream, CancellationToken cancellationToken)
        {
            if (hashImplementation == null)
            {
                throw new ArgumentNullException(nameof(hashImplementation));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var image = await Image.LoadAsync<Rgba32>(stream, cancellationToken);
            return hashImplementation.Hash(image);
        }
    }
    */
}
