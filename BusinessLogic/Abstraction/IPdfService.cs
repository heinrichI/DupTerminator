using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.BusinessLogic.Abstraction
{
    public interface IPdfService
    {
        string? CalculateHash(PdfFileInfo pdfInfo, Func<Stream, string?> calculator);
        IEnumerable<PdfFileInfo> GetInfos(ExtendedFileInfo item, CancellationToken cancelToken);
        Stream GetStream(PdfFileInfo pdfInfo);
        IList<(PdfFileInfo, Stream)> GetStreams(ExtendedFileInfo fileInfo, CancellationToken cancelToken);
    }
}
