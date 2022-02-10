using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;

using System.Linq;
using System.Collections;
using System.Diagnostics;
using DupTerminator.BusinessLogic;

#if NUNIT
using NUnit.Framework;
#endif

namespace DupTerminator
{
    class FileFunctions
    {
        #region "Declarations"
        private Thread thFileCount;

        private Dictionary<string, bool> _directorySearchList = new Dictionary<string, bool>();
        private List<string> _directorySkipList = new List<string>();
        private ArrayList _completeFileList = new ArrayList();
        private ArrayList _duplicateFileList = new ArrayList();

        private static bool _cancelSearch = false;
        private int _DuplicateCount = 0;
        private int _fileFoundCount = 0;
        private ulong _DuplicateFileSize = 0;
        private List<string> _includePattern = new List<string>();
        private List<string> _excludePattern = new List<string>();
        private string[] _separators = new string[] { "|", ";" };

        public Settings settings; //= new Settings(); //экземпляр класса с настройками 
        private readonly IDBManager _dbManager;
        #endregion //"Declarations"

        #region "Events"
        public delegate void FolderChangedDelegate(int i, string folder);  //параметры те же что в главной функции
        public event FolderChangedDelegate FolderChangedEvent;  //надо вызвать при смене директории

        public delegate void FileCountAvailableDelegate(int Number);
        public event FileCountAvailableDelegate FileCountAvailableEvent;

        public delegate void FileListAvailableDelegate(ArrayList fl);
        public event FileListAvailableDelegate FileListAvailableEvent;

        public delegate void DuplicateFileListAvailableDelegate(ArrayList duplicateFiles);
        public event DuplicateFileListAvailableDelegate DuplicateFileListAvailableEvent;

        public delegate void FileCheckInProgressDelegate(string fileName, int currentCount);
        public event FileCheckInProgressDelegate FileCheckInProgressEvent;

        public delegate void SearchCancelledDelegate();
        public event SearchCancelledDelegate SearchCancelledEvent;
        #endregion //"Events"

        public FileFunctions(IDBManager dbManager)
        {
            settings = Settings.GetInstance();
            _dbManager = dbManager ?? throw new ArgumentNullException(nameof(dbManager));
        }

        #region "Internal Helper Functions"
        /// <summary>
        /// Add all files in the requested directory to the filearraylist
        /// </summary>
        /// <param name="di">Directory from which to add files</param>
        /// <param name="returnFiles">ArrayList to add files to</param>
        private void AddFiles(System.IO.DirectoryInfo di, ref ArrayList returnFiles)
        {
            try
            {
                List<FileInfo> files = new List<FileInfo>();

                //расширения
                if (_includePattern.Count > 0)
                {
                    foreach (string pattern in _includePattern)
                        files.AddRange(di.GetFiles(pattern, SearchOption.TopDirectoryOnly));
                }
                else
                    files.AddRange(di.GetFiles());

                if (_excludePattern.Count > 0)
                {
                    List<FileInfo> excludeFiles = new List<FileInfo>();

                    foreach (string patternExclude in _excludePattern)
                        excludeFiles.AddRange(di.GetFiles(patternExclude, SearchOption.TopDirectoryOnly));

                    if (excludeFiles.Count != 0)
                    {
                        Debug.WriteLine("Не подошли по паттернам файлы: " + String.Join(", ", excludeFiles.Select(f => f.FullName).ToArray()));
                        int deleted = files.RemoveAll(delegate(FileInfo file)
                        {
                            return (excludeFiles.Any(f => f.FullName == file.FullName));
                        });
                        Debug.WriteLine("Удалено: " + deleted);
                        //if (deleted != excludeFiles.Count)
                        //    throw new Exception("Количество удаленных не равно количеству для удаления файлов");
                     }
                }

                //пропускаем не подходящие по размерам
                for (int i = 0; i < files.Count; i++)
                {
                    if (files[i].Length > settings.Fields.limits[0] && files[i].Length < settings.Fields.limits[1])
                    {
                        returnFiles.Add(new ExtendedFileInfo(files[i]));

                        if (settings.Fields.IsScanMax)
                            if (returnFiles.Count >= settings.Fields.MaxFile)
                            {
                                Debug.WriteLine("Слишком много файлов = " + returnFiles.Count);
                                return;
                            }
                    }
                }

                Debug.WriteLine(String.Format("Директория {0}, добавлено {1} файлов", di.FullName, returnFiles.Count));

                _fileFoundCount += files.Count;
                if (FolderChangedEvent != null)
                    FolderChangedEvent(_fileFoundCount, di.FullName);
                //m_EvSuspend.WaitOne(); //pause

                //_directoriesSearched.Add((string)di.FullName.ToString());
            }
            catch (System.IO.FileNotFoundException)
            {
                //not do
            }
            catch (System.UnauthorizedAccessException)
            {
                //not do
            }
        }

        /// <summary>
        /// Add all files from the requested directory to the filearraylist. If 
        /// isRecurse is true, add all file in sub directories as well.
        /// </summary>
        /// <param name="di">Directory from which to add files</param>
        /// <param name="al">ArrayList to add files to</param>
        /// <param name="isRecurse">Add all files in the subdirectories as well</param>
        private void AddAllFiles(System.IO.DirectoryInfo di, ref ArrayList al, bool isRecurse)
        {
            if (_cancelSearch == true)
            {
                if (SearchCancelledEvent != null)
                    SearchCancelledEvent();
                return;
            }

            try
            {
                //System.Diagnostics.Debug.WriteLine("AddFiles for directory " + di.FullName);
                AddFiles(di, ref al); //добавление файлов

                if (settings.Fields.IsScanMax)
                    if (al.Count >= settings.Fields.MaxFile)
                        return;

                //Add subdirectories
                if (isRecurse)
                {
                    System.IO.DirectoryInfo[] dList = di.GetDirectories();
                    for (int i = 0; i < dList.Length; i++)
                        /*if (!_directorySkipList.Contains(dList[i].FullName))
                            AddAllFiles(dList[i], ref al, isRecurse);*/
                        //if (_directorySkipList.BinarySearch(dList[i].FullName)<0)
                        //if (_directorySkipList.IndexOf(dList[i].FullName) != -1)
                        if (!_directorySkipList.Contains(dList[i].FullName, StringComparer.OrdinalIgnoreCase))
                            AddAllFiles(dList[i], ref al, isRecurse);
                        else
                            Debug.WriteLine(String.Format("Директория {0} есть в списке пропускаемых. Пропускаем.", dList[i].FullName));
                }
            }
            catch (PathTooLongException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message + Environment.NewLine + di.FullName, "Error in AddAllFiles", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                //throw;
            }
            catch (System.UnauthorizedAccessException)
            {
                //not do
            }
        }

        /// <summary>
        /// Count the number of files in the requested directory. If recurse is true count all files
        /// in current directory as well as all subdirectories.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="recurse"></param>
        /// <returns></returns>
        private int GetFileCount(string dir, bool recurse)
        {
            try
            {
                if (recurse)
                    return Directory.GetFiles(dir, "*.*", System.IO.SearchOption.AllDirectories).Length;
                else
                    return Directory.GetFiles(dir, "*.*", System.IO.SearchOption.TopDirectoryOnly).Length;
            }

            catch (UnauthorizedAccessException)
            {
                //Console.WriteLine(uae.Message);
                return 0;
            }
        }

        /// <summary>
        /// Return a collection of files grouped by size.
        /// </summary>
        /// <param name="alFiles">fileinfo arraylist</param>
        /// <returns>a collection of fileinfo array lists</returns>
        private ArrayList ParseBySize(ArrayList inputList)
        {
            long currentFileSize;
            ArrayList completeFileList = new ArrayList();
            ArrayList fileSizeMatchList = new ArrayList();

            if (inputList.Count <= 0)
                return null;

            inputList.Sort(new SortBySize());

            currentFileSize = ((ExtendedFileInfo)inputList[0]).fileInfo.Length;
            fileSizeMatchList.Add(inputList[0]);

            // Enumerate through all of the files in the listForCompare
            for (int i = 1; i < inputList.Count; i++)
            {
                if (currentFileSize == ((ExtendedFileInfo)inputList[i]).fileInfo.Length)
                {
                    fileSizeMatchList.Add(inputList[i]);  //содержит файлы с одинаковым размером
                }
                else
                {
                    completeFileList.Add(fileSizeMatchList.Clone());
                    fileSizeMatchList.Clear();
                    fileSizeMatchList.Add(inputList[i]);
                }
                currentFileSize = ((ExtendedFileInfo)inputList[i]).fileInfo.Length;
            }
            completeFileList.Add(fileSizeMatchList.Clone());
            return completeFileList;
        }

        private ArrayList ParseByName(ArrayList inputArray)
        {
            ArrayList fileNameMatchList = new ArrayList();
            ArrayList nameSortedGroup = new ArrayList();
            string currentFileName;

            foreach (System.Collections.ArrayList array in inputArray)
            {
                if (array.Count > 1)
                {
                    array.Sort(new SortByName());

                    currentFileName = ((ExtendedFileInfo)array[0]).fileInfo.Name;
                    fileNameMatchList.Add(array[0]);

                    // Enumerate through all of the files in the listForCompare
                    for (int i = 1; i < array.Count; i++)
                    {
                        if (string.Compare(currentFileName, ((ExtendedFileInfo)array[i]).fileInfo.Name) == 0)
                        {
                            fileNameMatchList.Add(array[i]);
                        }
                        else
                        {
                            nameSortedGroup.Add(fileNameMatchList.Clone());
                            fileNameMatchList.Clear();
                            fileNameMatchList.Add(array[i]);
                        }
                        currentFileName = ((ExtendedFileInfo)array[i]).fileInfo.Name;
                    }
                    nameSortedGroup.Add(fileNameMatchList.Clone());
                    fileNameMatchList.Clear();
                }
            }
            return nameSortedGroup;
        }

        /// <summary>
        /// Return a collection of files grouped by checksum.
        /// </summary>
        /// <param name="alFiles"></param>
        /// <returns></returns>
        private ArrayList ParseByChecksum(ArrayList alFiles)
        {
            string currentChecksum;
            ArrayList completeFileList = new ArrayList();
            ArrayList fileChecksumMatchList = new ArrayList();

            SortByChecksum sortByChecksum = new SortByChecksum(_dbManager);
            sortByChecksum.FastCheck = settings.Fields.FastCheck;
            sortByChecksum.FastCheckFileSize = settings.Fields.FastCheckFileSizeMb * 1024 * 1024;
            sortByChecksum.chunkSize = settings.Fields.FastCheckBufferKb * 1024;
            alFiles.Sort(sortByChecksum);
            currentChecksum = ((ExtendedFileInfo)alFiles[0]).GetCheckSum(_dbManager);
            fileChecksumMatchList.Add(alFiles[0]);
            for (int i = 1; i < alFiles.Count; i++)
            {
                if (String.IsNullOrEmpty(((ExtendedFileInfo)alFiles[i]).GetCheckSum(_dbManager)))
                    continue;
                if (string.Compare(currentChecksum, ((ExtendedFileInfo)alFiles[i]).GetCheckSum(_dbManager)) == 0)
                {
                    fileChecksumMatchList.Add(alFiles[i]);
                }
                else
                {
                    completeFileList.Add(fileChecksumMatchList.Clone());
                    fileChecksumMatchList.Clear();
                    fileChecksumMatchList.Add(alFiles[i]);
                }
                currentChecksum = ((ExtendedFileInfo)alFiles[i]).GetCheckSum(_dbManager);
            }
            completeFileList.Add(fileChecksumMatchList.Clone());
            return completeFileList;
        }
        #endregion //"Internal Helper Functions"

        #region "Public Methods"
        /// <summary>
        /// Start the search process for duplicate files.
        /// </summary>
        public void BeginSearch()
        {
            //Thread thFileCount = new Thread(ScanForDuplicates);
            thFileCount = new Thread(ScanForDuplicates);
            thFileCount.Name = "DupTerminator: File Comparison";

            //Start the file search and check on a new thread.
            thFileCount.Start();
        }

        public void PauseSearch()
        {
            thFileCount.Suspend();
            //m_EvSuspend.Reset();
        }

        public void ResumeSearch()
        {
            thFileCount.Resume();
            //m_EvSuspend.Set();
        }

        private void ScanForDuplicates()
        {
            //System.Diagnostics.Debug.WriteLine("ScanForDuplicates dbManager.Active=" + dbManager.Active);
            Clear_Results();

            if (!String.IsNullOrEmpty(settings.Fields.IncludePattern))
                ParsePattern(settings.Fields.IncludePattern, ref _includePattern);
            if (!String.IsNullOrEmpty(settings.Fields.ExcludePattern))
                ParsePattern(settings.Fields.ExcludePattern, ref _excludePattern);
            if ((_includePattern.Count > 0) && (_excludePattern.Count > 0))
            {
                foreach (string forbiddenPattern in _excludePattern)
                    if (_includePattern.Contains(forbiddenPattern))
                        _includePattern.Remove(forbiddenPattern);
            }

            Debug.WriteLine("Паттерны включения: " + String.Join(", ", _includePattern.ToArray()));
            Debug.WriteLine("Паттерны исключения: " + String.Join(", ", _excludePattern.ToArray()));

            if (settings.Fields.UseDB)
            {
                _dbManager.Active = true;
                _dbManager.CreateDataBase();
            }
            //System.Diagnostics.Debug.WriteLine("ScanForDuplicates dbManager.Active=" + dbManager.Active);

            // Get the total number of files we are going to check.
            foreach (KeyValuePair<string, bool> dItem in _directorySearchList)
            {
                DirectoryInfo di = new System.IO.DirectoryInfo(dItem.Key);

                //System.Diagnostics.Debug.WriteLine("ScanForDuplicates AddAllFiles for directory dItem.Key=" + dItem.Key);
                //System.Diagnostics.Debug.WriteLine("ScanForDuplicates AddAllFiles for directory di.FullName=" + di.FullName);
                AddAllFiles(di, ref _completeFileList, dItem.Value);
                Debug.WriteLine(String.Format("Директория {0}, общее количество добавленных файлов: {1}", dItem.Key, _completeFileList.Count));
                di = null;
            }

            if (FileCountAvailableEvent != null)
                FileCountAvailableEvent(_completeFileList.Count);  //событие event ot main form
            if (FileListAvailableEvent != null)
                FileListAvailableEvent(_completeFileList);  //Add_LVI(

            ArrayList fileList;
            fileList = ParseBySize(_completeFileList); //GetGroup
            if (settings.Fields.IsSameFileName)
                fileList = ParseByName(fileList);

            if (fileList == null)
            {
                if (DuplicateFileListAvailableEvent != null)
                    DuplicateFileListAvailableEvent(_duplicateFileList);
                return;
            }

            int currentFileCount = 0;
            ArrayList ChecksumSortedGroup;

            // Run through all like sized _groups and check for duplicate checksums
            foreach (ArrayList ssg in fileList)
            {
                currentFileCount += ssg.Count;  //currentFileCount - progressBar
                if (ssg.Count > 1)
                {
                    ChecksumSortedGroup = ParseByChecksum(ssg);
                    foreach (ArrayList csg in ChecksumSortedGroup)
                    {
                        if (csg.Count > 1)
                        {
                            foreach (ExtendedFileInfo efiGroup in csg)
                            {
                                if (_cancelSearch == true)
                                {
                                    if (_dbManager.Active)
                                        _dbManager.SaveFromMemory();
                                    
                                    if (_duplicateFileList.Count > 0)
                                        DuplicateFileListAvailableEvent(_duplicateFileList);
                                    else if(SearchCancelledEvent != null)
                                        SearchCancelledEvent();
                                    //m_EvSuspend.Close();
                                    return;
                                }

                                if (FileCheckInProgressEvent != null)
                                    FileCheckInProgressEvent(efiGroup.fileInfo.FullName, currentFileCount);
                                _duplicateFileList.Add(efiGroup);
                                _DuplicateFileSize += Convert.ToUInt64(efiGroup.fileInfo.Length);
                                _DuplicateCount++;

                                //currentFileSize = efiGroup.fileInfo.Length;
                            }
                            //_DuplicateFileSize += currentFileSize;
                            //m_EvSuspend.WaitOne(); //pause
                        }
                    }
                }
            }

            if (_dbManager.Active)
                _dbManager.SaveFromMemory();

            if (DuplicateFileListAvailableEvent != null)
                DuplicateFileListAvailableEvent(_duplicateFileList);
        }

        private void ParsePattern(string pattern, ref List<string> outpattern)
        //private List<string> ParsePattern(string pattern)
        {
            //m_SkipPattern = new List<string>();
            //m_Pattern = new List<string>();
            //List<string> outpattern = new List<string>();
            string[] tmpPattern = pattern.Split(_separators, StringSplitOptions.RemoveEmptyEntries);

            outpattern.AddRange(tmpPattern);
            //foreach (string minipattern in tmpPattern)
            //    outpattern.Add(minipattern.Trim());
           // return outpattern;
        }

        /// <summary>
        /// Add a new search directory.
        /// </summary>
        /// <param name="dirName"></param>
        public void Add_Search_Directory(string dirName, bool isSubDir)
        {
            if (System.IO.Directory.Exists(dirName))
            {
                if (!_directorySearchList.ContainsKey(dirName))
                {
                    //System.Diagnostics.Debug.WriteLine("Directory added in _directorySearchList " + dirName);
                    _directorySearchList.Add(dirName, isSubDir);
                }
            }
        }

        /// <summary>
        /// Add a new skip directory
        /// </summary>
        /// <param name="dirName"></param>
        public void Add_Skip_Directory(string dirName)
        {
            if (Directory.Exists(dirName))
            {
                DeleteLastSlash(ref dirName);
                if (!_directorySkipList.Contains(dirName))
                {
                    _directorySkipList.Add(dirName);
                    //if (DirectoryListUpdate != null)
                    //    DirectoryListUpdate(_directorySearchList);
                }
            }
        }

        private void DeleteLastSlash(ref string path)
        {
            if (path[path.Length - 1] == Path.DirectorySeparatorChar)
                path = path.Remove(path.Length - 1);
        }

        /// <summary>
        /// Remove an existing directory
        /// </summary>
        /// <param name="dirName"></param>
        public void Remove_Search_Directory(string dirName)
        {
            if (_directorySearchList == null)
                return;

            if (_directorySearchList.ContainsKey(dirName))
                _directorySearchList.Remove(dirName);

            return;
        }

        /// <summary>
        /// Clear the search directory.
        /// </summary>
        public void Clear_Search_Directory()
        {
            _directorySearchList.Clear();
        }

        public void Clear_Skip_Directory()
        {
            _directorySkipList.Clear();
        }

        public void Clear_Results()
        {
            _cancelSearch = false;
            _completeFileList.Clear();
            _duplicateFileList.Clear();
            //_directoriesSearched.Clear();
            //_groupCount = 0;
            //_totalDuplicateFileSize = 0;
            _DuplicateFileSize = 0;
            //if (_includePattern != null) _includePattern.Clear();
            //if (_excludePattern != null) _excludePattern.Clear();
            _includePattern.Clear();
            _excludePattern.Clear();
        }

        public void CancelSearch()
        {
            _cancelSearch = true;
            //m_EvSuspend.Close();
        }
        #endregion // "Methods"

        #region "Public Properties"
        /// <summary>
        /// Return the total number of files to compare
        /// </summary>
        public  double TotalFiles
        {
            get 
            {
                if (_completeFileList != null)
                    return _completeFileList.Count;
                else
                    return 0;
            }
        }

        /// <summary>
        /// Return total duplicate files found after search
        /// </summary>
        public double DuplicateFileCount
        {
            get
            {
                if (_duplicateFileList != null)
                    return _duplicateFileList.Count;
                else
                    return 0;
            }
        }

        /// <summary>
        /// Total number of duplicate file groups
        /// </summary>
        /*public double DuplicateFileGroupCount
        {
            get { return _groupCount; }
        }*/

        /// <summary>
        /// Amount of saved disk space in bytes
        /// </summary>
        public ulong DuplicateFileSize
        {
            get { return _DuplicateFileSize; }
        }

        /// <summary>
        /// Return a listForCompare of directories that are going to be searched
        /// </summary>
        //public ArrayList DirectorySearchList
        public Dictionary<string, bool> DirectorySearchList
        {
            get { return _directorySearchList; }
        }
        #endregion // "Properties

    }



#if NUNIT
    [TestFixture]
    public class TestFileFunctions
    {
        [Test]
        //[ExpectedException(typeof(NullReferenceException))]
        public void Create()
        {
            FileFunctions fFunctions = new FileFunctions();

            fFunctions.Add_Search_Directory("..\\..\\..\\TestDir", true);

            //fFunctions.BeginSearch();
        }
    }
#endif

}
