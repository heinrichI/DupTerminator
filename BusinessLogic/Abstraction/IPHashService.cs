using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Abstraction
{
    public interface IPHashService
    {
        (ulong phash, int width, int height) CalculatePHash(Stream stream);

        (ulong phash, int width, int height) CalculatePHash(string? path);

        bool IsSupportedExtension(string extension);
    }
}
