using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Model;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Logging;

namespace DupTerminator.Pdf
{
    internal class PdfService : IPdfService
    {
        private readonly ILogger<PdfService> _logger;
        ParsingOptions _parsingOption = new ParsingOptions()
        {
            UseLenientParsing = true,
            SkipMissingFonts = true,
            FilterProvider = MyFilterProvider.Instance
        };

        public PdfService(ILogger<PdfService> logger)
        {
            _logger = logger;
        }

        public string? CalculateHash(PdfFileInfo pdfInfo, Func<Stream, string?> calculator)
        {
            Debug.Assert(pdfInfo != null);
            using (var doc = PdfDocument.Open(pdfInfo.Container.Path, _parsingOption))
            {
                var page = doc.GetPage(int.Parse(pdfInfo.Name));

                var images = page.GetImages();
                IPdfImage image = pdfInfo.ImageIndex.HasValue ? images.Skip(pdfInfo.ImageIndex.Value).First() : images.First();
                Debug.Assert(image.RawBytes.Length == (int)pdfInfo.Size);

                using (var entryStream = new ChunkedMemoryStream(image.RawBytes.Length))
                {
                    entryStream.Write(image.RawBytes);
                    entryStream.Position = 0;
                    return calculator(entryStream);
                }
            }
        }

        public T[] CalculateHashes<T>(PdfFileInfo[] pdfFileInfos, Func<Stream, T> calculator)
        {
            List<T> result = new List<T>();
            using (var doc = PdfDocument.Open(pdfFileInfos[0].Container.Path, _parsingOption))
            {
                foreach (var pdfInfo in pdfFileInfos)
                {
                    var page = doc.GetPage(int.Parse(pdfInfo.Name));

                    var images = page.GetImages();
                    IPdfImage image = pdfInfo.ImageIndex.HasValue ? images.Skip(pdfInfo.ImageIndex.Value).First() : images.First();
                    Debug.Assert(image.RawBytes.Length == (int)pdfInfo.Size);

                    using (var entryStream = new ChunkedMemoryStream(image.RawBytes.Length))
                    {
                        entryStream.Write(image.RawBytes);
                        entryStream.Position = 0;
                        result.Add(calculator(entryStream));
                    }
                }
            }
            return result.ToArray();
        }

        public IEnumerable<PdfFileInfo> GetInfos(ExtendedFileInfo fileInfo, CancellationToken cancelToken)
        {
            List<PdfFileInfo> infos = new List<PdfFileInfo>();
            using (var doc = PdfDocument.Open(fileInfo.Path, _parsingOption))
            {
                foreach (var page in doc.GetPages())
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        System.Diagnostics.Debug.WriteLine("CollectImageStreams was canceled.");
                        break;
                    }

                    int imageIndex = 0;
                    try
                    {
                        foreach (var pdfImage in page.GetImages())
                        {
                            PdfFileInfo efi = new PdfFileInfo()
                            {
                                ImageIndex = imageIndex,
                                //LastAccessTime = entry.LastAccessTime,
                                Name = page.Number.ToString(),
                                //Extension = Path.GetExtension(entry.FileName),
                                Size = (ulong)pdfImage.RawBytes.Length,
                                Path = $"{fileInfo.Path}\\{page.Number}",
                                Container = fileInfo,
                                //ArchiveInArchive = archiveInArchive
                            };
                            imageIndex++;

                            var entryStream = new ChunkedMemoryStream(pdfImage.RawBytes.Length);
                            entryStream.Write(pdfImage.RawBytes);
                            entryStream.Position = 0;

                            infos.Add(efi);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{fileInfo.Path}: {ex.Message}");
                    }
                }
            }
            foreach (var item in infos)
            {
                item.ContainerFilesCount = infos.Count;
            }

            return infos;
        }

        public Stream GetStream(PdfFileInfo pdfInfo)
        {
            Debug.Assert(pdfInfo != null);
            using (var doc = PdfDocument.Open(pdfInfo.Container.Path, _parsingOption))
            {
                var page = doc.GetPage(int.Parse(pdfInfo.Name));

                var images = page.GetImages();
                Debug.Assert(images.Count() == 1);

                var entryStream = new ChunkedMemoryStream(images.First().RawBytes.Length);
                entryStream.Write(images.First().RawBytes);
                entryStream.Position = 0;
                return entryStream;
            }
        }

        public IList<(PdfFileInfo, Stream)> GetStreams(ExtendedFileInfo fileInfo, CancellationToken cancelToken)
        {
            List<(PdfFileInfo, Stream)> streams = new List<(PdfFileInfo, Stream)>();
            using (var doc = PdfDocument.Open(fileInfo.Path, _parsingOption))
            {
                foreach (var page in doc.GetPages())
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        System.Diagnostics.Debug.WriteLine("CollectImageStreams was canceled.");
                        break;
                    }

                    //foreach (var pdfImage in page.GetImages())
                    //{
                    //    bool result = pdfImage.TryGetPng(out var bytes);

                    //    File.WriteAllBytes($"image_{i++}.jpeg", bytes);
                    //}
                    foreach (var pdfImage in page.GetImages())
                    {
                        //File.WriteAllBytes($"{fileInfo.Name}_FromPDF.jpeg", pdfImage.RawBytes.ToArray());
                        PdfFileInfo efi = new PdfFileInfo()
                        {
                            //InArchive = true,
                            //LastAccessTime = entry.LastAccessTime,
                            Name = page.Number.ToString(),
                            //Extension = Path.GetExtension(entry.FileName),
                            Size = (ulong)pdfImage.RawBytes.Length,
                            Path = $"{fileInfo.Path}\\{page.Number}",
                            Container = fileInfo,
                            //ArchiveInArchive = archiveInArchive
                        };

                        var entryStream = new ChunkedMemoryStream(pdfImage.RawBytes.Length);
                        entryStream.Write(pdfImage.RawBytes);
                        entryStream.Position = 0;

                        streams.Add((efi, entryStream));
                    }
                }
            }
            foreach (var item in streams)
            {
                item.Item1.ContainerFilesCount = streams.Count;
            }

            return streams;
        }
    }
}
