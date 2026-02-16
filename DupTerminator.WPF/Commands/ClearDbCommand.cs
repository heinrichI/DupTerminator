using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DupTerminator.DataBase;

namespace DupTerminator.WPF.Commands
{
    internal class ClearDbCommand : ICommand
    {
        private readonly IArchiveInfoRepository _archiveInfoRepository;
        private readonly IPhashRepository _phashRepository;

        public ClearDbCommand(IArchiveInfoRepository archiveInfoRepository, IPhashRepository phashRepository)
        {
            _archiveInfoRepository = archiveInfoRepository;
            _phashRepository = phashRepository;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _archiveInfoRepository is not null;
        }

        public void Execute(object? parameter)
        {
            // Получаем список существующих дисков и преобразуем в HashSet для быстрого поиска
            var disks = new HashSet<string>(GetExistDisks(), StringComparer.OrdinalIgnoreCase);

            bool needVacum = false;
            int deleteCount = 0;
            var allPaths = _archiveInfoRepository.EnumerateAllPath().ToArray();
            foreach (string path in allPaths)
            {
                // Получаем корень пути (например, "C:\\")
                string? root = Path.GetPathRoot(path);

                //если диск из пути есть в существующих в системе дисках то проверить что путь сущестует
                if (root != null && disks.Contains(root) && !File.Exists(path))
                {
                    _archiveInfoRepository.DeleteByPath(path);
                    needVacum = true;
                    deleteCount++;
                }
            }

            allPaths = _phashRepository.GetAllContainerPath();
            foreach (string path in allPaths)
            {
                // Получаем корень пути (например, "C:\\")
                string? root = Path.GetPathRoot(path);

                //если диск из пути есть в существующих в системе дисках то проверить что путь сущестует
                if (root != null && disks.Contains(root) && !File.Exists(path))
                {
                    _phashRepository.DeleteByPath(path);
                    needVacum = true;
                    deleteCount++;
                }
            }
            if (needVacum)
            {
                _archiveInfoRepository.VacuumDatabase();
                MessageBox.Show($"Удалено {deleteCount} записей");
            }
        }

        // Пример реализации GetExistDisks
        private IEnumerable<string> GetExistDisks()
        {
            return DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => d.RootDirectory.FullName);
        }
    }
}
