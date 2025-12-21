using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.DataBase;

namespace DupTerminator.BusinessLogic.Service
{
   /* public class DbArchiveService
    {
        private readonly IArchiveService _archiveService;
        private readonly IExtendedFileInfoRepository _extendedFileInfoRepository;

        public DbArchiveService(
            IArchiveService archiveService,
            IExtendedFileInfoRepository extendedFileInfoRepository)
        {
            _archiveService = archiveService;
            _extendedFileInfoRepository = extendedFileInfoRepository;
        }

        internal IEnumerable<ExtendedFileInfo>? Get(bool useDB, ExtendedFileInfo item, CancellationToken token)
        {
            if (useDB)
            {
                var result = _extendedFileInfoRepository.Get(item.Path, item.LastWriteTime, item.Size);
                if (result == null)
                {
                    result = _archiveService.GetInfoFromArchive(item.Path, item, token);
                    if (result is not null && result.Any())
                    {
                        _extendedFileInfoRepository.Add(item, result);
                    }
                }
                if (string.IsNullOrEmpty(result.FirstOrDefault().Name))
                    Debug.WriteLine("_extendedFileInfoRepository return empty!");
                return result;
            }
            else
            {
                return _archiveService.GetInfoFromArchive(item.Path, item, token);
            }
        }
    }*/
}
