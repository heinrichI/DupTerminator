using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DupTerminator.DataBase
{
    internal static class JsonHelper
    {
        internal static Stream CompressJsonDataToStream<T>(T data, JsonSerializerOptions options)
        {
            var outputStream = new MemoryStream();
            var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal);

            // Create a UTF8JsonWriter that writes directly to the GZipStream
            using var jsonWriter = new Utf8JsonWriter(gzipStream);

            JsonSerializer.Serialize(jsonWriter, data, options);
            //jsonWriter.WriteStartArray();
            //foreach (var item in data)
            //{
            //JsonSerializer.Serialize(jsonWriter, item, options);
            //}
            //jsonWriter.WriteEndArray();

            jsonWriter.Flush();
            //gzipStream.Close(); // This also flushes the gzip stream

            outputStream.Position = 0; // Reset position for reading
            return outputStream;
        }

        internal static Stream CompressJsonDataToStream2<T>(T data, JsonSerializerOptions options)
        {
            // 1. Serialize the object to a UTF-8 byte array
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data, options);

            // 2. Compress the byte array using GZipStream
            var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
            {
                gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
            }
            // The compressed data is now in the outputStream
            return outputStream;
        }

        internal static byte[] CompressJsonData<T>(T data, JsonSerializerOptions options)
        {
            // 1. Serialize the object to a UTF-8 byte array
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data, options);

            // 2. Compress the byte array using GZipStream
            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
                {
                    gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
                }
                // The compressed data is now in the outputStream
                return outputStream.ToArray();
            }
        }
    }
}
