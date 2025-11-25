using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.BusinessLogic.Abstraction
{
    /// <summary>
    /// Multi-index hashing
    /// </summary>
    public interface IMIH : IDisposable
    {
        void Update(IDictionary<ulong, IList<PHashFileInfo>> newHashes);

        void Train(int wordLength = 16, int threshold = 7);

        IEnumerable<(ulong Hash, List<PHashFileInfo> FileInfos, int HammingDistance)> Query(ulong hash);
    }
}
