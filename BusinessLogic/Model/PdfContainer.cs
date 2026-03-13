using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    public class PdfContainer : ContainerInfo
    {
        public PdfContainer() { }

        public PdfContainer(ExtendedFileInfo efi)
        {
            Path = efi.Path;
            Name = efi.Name;
            Size = efi.Size;
            Extension = efi.Extension;
            LastWriteTime = efi.LastWriteTime;
        }
    }
}
