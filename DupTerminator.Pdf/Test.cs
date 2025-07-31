using System.Collections.Generic;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Filters.Dct.JpegLibrary;
using UglyToad.PdfPig.Filters.Jbig2.PdfboxJbig2;
using UglyToad.PdfPig.Filters.Jpx.OpenJpeg;
using UglyToad.PdfPig.Tokens;

namespace DupTerminator.Pdf
{
    public class Test
    {
        public static void TestPdf()
        {
            var parsingOption = new ParsingOptions()
            {
                UseLenientParsing = true,
                SkipMissingFonts = true,
                FilterProvider = MyFilterProvider.Instance
            };

            using (var doc = PdfDocument.Open("f:\\E\\SourceC#My\\DupTest\\Papyrus T22 - La Prisonniere de Sekhmet  [De Gieter].pdf", parsingOption))
            {
                int i = 0;
                foreach (var page in doc.GetPages())
                {
                    //foreach (var pdfImage in page.GetImages())
                    //{
                    //    bool result = pdfImage.TryGetPng(out var bytes);

                    //    File.WriteAllBytes($"image_{i++}.jpeg", bytes);
                    //}
                    foreach (var pdfImage in page.GetImages())
                    {                        
                        // Process your images, e.g.:
                        File.WriteAllBytes($"image_{i++}.jpg", pdfImage.RawBytes.ToArray());
                    }
                }
            }
        }
    }
}
