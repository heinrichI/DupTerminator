using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Filters.Dct.JpegLibrary;
using UglyToad.PdfPig.Filters.Jbig2.PdfboxJbig2;
using UglyToad.PdfPig.Filters.Jpx.OpenJpeg;
using UglyToad.PdfPig.Tokens;

namespace DupTerminator.Pdf
{
    /// <summary>
    /// Filter provider to add support for JBIG2, DCT and JPX filters.
    /// </summary>
    public sealed class MyFilterProvider : BaseFilterProvider
    {
        /// <summary>
        /// The single instance of this provider.
        /// </summary>
        public static readonly MyFilterProvider Instance = new MyFilterProvider();

        /// <inheritdoc/>
        private MyFilterProvider() : base(GetDictionary())
        {
        }

        private static Dictionary<string, IFilter> GetDictionary()
        {
            // New filters
            var dct = new JpegLibraryDctDecodeFilter();
            //var jbig2 = new PdfboxJbig2DecodeFilter();
            //var jpx = new OpenJpegJpxDecodeFilter();

            // Standard PdfPig filters
            var ascii85 = new Ascii85Filter();
            var asciiHex = new AsciiHexDecodeFilter();
            var ccitt = new CcittFaxDecodeFilter();
            var flate = new FlateFilter();
            var runLength = new RunLengthFilter();
            var lzw = new LzwFilter();


            return new Dictionary<string, IFilter>
               {
                   { NameToken.Ascii85Decode.Data, ascii85 },
                   { NameToken.Ascii85DecodeAbbreviation.Data, ascii85 },
                   { NameToken.AsciiHexDecode.Data, asciiHex },
                   { NameToken.AsciiHexDecodeAbbreviation.Data, asciiHex },
                   { NameToken.CcittfaxDecode.Data, ccitt },
                   { NameToken.CcittfaxDecodeAbbreviation.Data, ccitt },
                   { NameToken.DctDecode.Data, dct },
                   { NameToken.DctDecodeAbbreviation.Data, dct },
                   { NameToken.FlateDecode.Data, flate },
                   { NameToken.FlateDecodeAbbreviation.Data, flate },
                   //{ NameToken.Jbig2Decode.Data, jbig2 },
                   //{ NameToken.JpxDecode.Data, jpx },
                   { NameToken.RunLengthDecode.Data, runLength },
                   { NameToken.RunLengthDecodeAbbreviation.Data, runLength },
                   { NameToken.LzwDecode.Data, lzw },
                   { NameToken.LzwDecodeAbbreviation.Data, lzw }
               };
        }
    }
}