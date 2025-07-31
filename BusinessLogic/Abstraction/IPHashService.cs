using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Abstraction
{
    public interface IPHashService
    {
        ulong CalculatePHash(Stream stream);

        ulong CalculatePHash(string? path);

        bool IsSupportedExtension(string extension);
    }
}
