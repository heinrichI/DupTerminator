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
                var page = doc.GetPage(pdfInfo.PageNumber);

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

        public (PdfFileInfo, T)[] CalculateHashes<T>(PdfFileInfo[] pdfFileInfos, Func<Stream, T> calculator)
        {
            var sorted = pdfFileInfos.OrderBy(p => p.Name).ThenBy(p => p.ImageIndex);
            List<(PdfFileInfo, T)> result = new List<(PdfFileInfo, T)>(pdfFileInfos.Length);
            using (var doc = PdfDocument.Open(pdfFileInfos[0].Container.Path, _parsingOption))
            {
                Page? lastPage = null;
                int? lastPageIndex = null;
                IPdfImage[]? lastPageImages = null;
                foreach (var pdfInfo in sorted)
                {
                    int pageIndex = pdfInfo.PageNumber;
                    Page? page;
                    IPdfImage[]? images;
                    if (lastPageIndex.HasValue && lastPageIndex.Value == pageIndex)
                    {
                        page = lastPage;
                        images = lastPageImages;
                    }
                    else
                    {
                        page = doc.GetPage(pageIndex);
                        lastPage = page;
                        lastPageIndex = pageIndex;
                        images = page.GetImages().ToArray();
                        lastPageImages = images;
                    }

                    //IPdfImage image = pdfInfo.ImageIndex.HasValue ? images.Skip(pdfInfo.ImageIndex.Value).First() : images.First();
                    IPdfImage image = pdfInfo.ImageIndex.HasValue ? images[pdfInfo.ImageIndex.Value] : images[0];
                    Debug.Assert(image.RawBytes.Length == (int)pdfInfo.Size);

                    using (var entryStream = new ChunkedMemoryStream(image.RawBytes.Length))
                    {
                        entryStream.Write(image.RawBytes);
                        entryStream.Position = 0;
                        result.Add((pdfInfo, calculator(entryStream)));
                    }
                }
            }
            return result.ToArray();
        }

        public IEnumerable<PdfFileInfo> GetInfos(ExtendedFileInfo fileInfo, CancellationToken cancelToken)
        {
            List<PdfFileInfo> infos = new List<PdfFileInfo>();
            var container = new PdfContainer(fileInfo);
            try
            {
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
                                    PageNumber = page.Number,
                                    ImageIndex = imageIndex,
                                    Name = $"{page.Number}.{imageIndex}",
                                    Size = (ulong)pdfImage.RawBytes.Length,
                                    Path = $"{fileInfo.Path}\\{page.Number}.{imageIndex}",
                                    Container = container,
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
                            _logger.LogError(ex, string.Intern($"{fileInfo.Path}: {ex.Message}"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Intern($"{fileInfo.Path}: {ex.Message}"));
            }

            container.Files = infos.Select(i => new SimpleFileInfo(i)).ToArray();

            return infos;
        }

        public Stream GetStream(PdfFileInfo pdfInfo)
        {
            Debug.Assert(pdfInfo != null);
            using (var doc = PdfDocument.Open(pdfInfo.Container.Path, _parsingOption))
            {
                var page = doc.GetPage(pdfInfo.PageNumber);

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
            var container = new PdfContainer(fileInfo);
            try
            {
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
                        int imageIndex = 0;
                        foreach (var pdfImage in page.GetImages())
                        {
                            //File.WriteAllBytes($"{fileInfo.Name}_FromPDF.jpeg", pdfImage.RawBytes.ToArray());
                            PdfFileInfo efi = new PdfFileInfo()
                            {
                                PageNumber = page.Number,
                                ImageIndex = imageIndex,
                                Name = $"{page.Number}.{imageIndex}",
                                Size = (ulong)pdfImage.RawBytes.Length,
                                Path = $"{fileInfo.Path}\\{page.Number}.{imageIndex}",
                                Container = container,
                            };
                            imageIndex++;

                            var entryStream = new ChunkedMemoryStream(pdfImage.RawBytes.Length);
                            entryStream.Write(pdfImage.RawBytes);
                            entryStream.Position = 0;

                            streams.Add((efi, entryStream));
                        }
                    }
                }

                container.Files = streams.Select(i => new SimpleFileInfo(i.Item1)).ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Intern($"{fileInfo.Path}: {ex.Message}"));
            }           

            return streams;
        }
    }
}
