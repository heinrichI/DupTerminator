using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Threading;
using System.ComponentModel;
using DupTerminator.Views;

namespace DupTerminator
{
    //для Load_Save ListViewDuplicate
    [Serializable]
    //[XmlInclude(typeof(ListViewItemSave))]
    public class ListViewSave
    {
        [NonSerialized]
        private List<GroupOfDupl> _groups; //список групп для сортировки
        //[NonSerialized]
        //private bool _stopAction = false;

        [XmlArray("Items")]
        public List<ListViewItemSave> Items; //список элементов для отображения

        public ListViewSave()
        {
            _groups = new List<GroupOfDupl>();
            Items = new List<ListViewItemSave>();
        }

        public ListViewSave(int col)
        {
            _groups = new List<GroupOfDupl>();
            Items = new List<ListViewItemSave>(col);
        }

        public ListViewSave(ListViewSave lvs)
        {
            _groups = new List<GroupOfDupl>();
            Items = new List<ListViewItemSave>();
            Items.AddRange(lvs.Items);
        }

        public ListViewSave Clone()
        {
            return new ListViewSave(this);
        }

        public void Add(ExtendedFileInfo efi)
        {
            //if (!Groups.Contains(efi.CheckSum))
            //     Groups.Add(efi.CheckSum);

            ListViewItemSave itemLVSave = new ListViewItemSave(6);
            itemLVSave.Name = "FileName";
            itemLVSave.Text = efi.fileInfo.Name;
            itemLVSave.Group = efi.CheckSum;
            itemLVSave.Checked = false;
            itemLVSave.Color = Settings.GetInstance().Fields.ColorRow1;

            ListViewItemSaveSubItem subItem = new ListViewItemSaveSubItem();
            subItem.Name = "FileName";
            subItem.Text = efi.fileInfo.Name;
            itemLVSave.SubItems[0] = subItem;

            subItem = new ListViewItemSaveSubItem();
            subItem.Name = "Directory";
            subItem.Text = efi.fileInfo.DirectoryName;
            itemLVSave.SubItems[1] = subItem;

            subItem = new ListViewItemSaveSubItem();
            subItem.Name = "Size";
            subItem.Text = efi.fileInfo.Length.ToString();
            itemLVSave.SubItems[2] = subItem;

            subItem = new ListViewItemSaveSubItem();
            subItem.Name = "FileType";
            //subItem.Text = efi.fileInfo.Extension.Replace(".", string.Empty).ToUpper() + " File";
            subItem.Text = efi.fileInfo.Extension.Replace(".", string.Empty).ToUpper();
            itemLVSave.SubItems[3] = subItem;

            subItem = new ListViewItemSaveSubItem();
            subItem.Name = "LastAccessed";
            subItem.Text = efi.fileInfo.LastAccessTime.ToString();
            itemLVSave.SubItems[4] = subItem;

            subItem = new ListViewItemSaveSubItem();
            subItem.Name = "MD5Checksum";
            subItem.Text = efi.CheckSum;
            itemLVSave.SubItems[5] = subItem;

            if (!Items.Contains(itemLVSave))
                Items.Add(itemLVSave);
        }

        public void Clear()
        {
            Items.Clear();
            //Groups.Clear();
        }

        /// <summary>
        /// Get path (with file name).
        /// </summary>
        public string GetPath(int index)
        {
            return System.IO.Path.Combine(Items[index].Directory, Items[index].FileName);
        }

        public string GetFileName(int index)
        {
            return Items[index].FileName;
        }

        public string GetDirectory(int index)
        {
            return Items[index].Directory;
        }

        public void SetDirectory(int index, string path)
        {
            Items[index].Directory = path;
        }

        public List<int> GetGroupIndexs(string group)
        {
            List<int> indexs = new List<int>();
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Group == group)
                {
                    indexs.Add(i);
                }
            }
            return indexs;
        }

        public IEnumerable<ListViewItemSave> CheckedItems()
        {
            //get { return m_lSeqs; }
            foreach (ListViewItemSave item in Items)
            {
                if (item.Checked)
                {
                    yield return item;
                }
            }
        }

        public List<ListViewItemSave> GetCheckedList()
        {
            List<ListViewItemSave> checkedList = new List<ListViewItemSave>();
            foreach (ListViewItemSave item in Items)
            {
                if (item.Checked)
                    checkedList.Add(item);
            }
            return checkedList;
        }

        /// <summary>
        /// Возвращает список файлов переданной группы.
        /// </summary>
        private List<string> GetListOfThisFolder(string group)
        {
            List<string> listOfThisFolder = new List<string>();
            foreach (ListViewItemSave item in Items)
            {
                if (item.Group == group)
                    listOfThisFolder.Add(item.Directory);
            }
            return listOfThisFolder;
        }

        /// <summary>
        /// Создаем словарь групп.
        /// </summary>
        private Dictionary<string, List<ListViewItemSave>> GetDictionaryOfGroups()
        {
            Dictionary<string, List<ListViewItemSave>> groups = new Dictionary<string, List<ListViewItemSave>>();
            foreach (ListViewItemSave item in Items)
            {
                if (!groups.ContainsKey(item.Group))
                    groups.Add(item.Group, new List<ListViewItemSave>());
                groups[item.Group].Add(item);
            }
            return groups;
        }

        private void FillItemsFromDictionary(Dictionary<string, List<ListViewItemSave>> groups)
        {
            Items.Clear();
            foreach (KeyValuePair<string, List<ListViewItemSave>> keyValuePair in groups)
            {
                Items.AddRange(keyValuePair.Value);
            }

            ColoringOfGroups();
        }

        #region Check

        public void CheckAllInThisFolderinGroupWithThisFolders(int index)
        {
            string pathForChecking = GetDirectory(index);
            List<string> listOfPathsForCompare = GetListOfThisFolder(Items[index].Group);

            //Dictionary<string, List<ListViewItemSave>> groups = DictionaryOfGroups();
            UpdateListOfGroups();

            //проверяем каждую группу на наличие ВСЕХ записей из искомой
            //foreach (KeyValuePair<string, List<ListViewItemSave>> group in groups)
            foreach (GroupOfDupl group in _groups)
            {
                bool check = true;
                foreach (string compared in listOfPathsForCompare) //в искомой
                {
                    bool groupApproaches = false;
                    //foreach (ListViewItemSave item in group.Value) //в текущей
                    foreach (ListViewItemSave item in group.Items) //в текущей
                    {
                        if (String.Compare(compared, item.Directory) == 0) //path equal
                        {
                            groupApproaches = true;
                            break;
                        }
                    }
                    //если не найдена то пропускаем
                    if (!groupApproaches)
                    {
                        check = false; //группа не подходит
                        break; //выход из общего цикла
                    }

                }
                if (check) //помечаем
                {
                    foreach (ListViewItemSave item in group.Items)
                    {
                        if (item.Directory == pathForChecking) //&& (item.GetGroup == group.Key))
                            item.Checked = true;
                    }
                }
            }

            ColoringAllCheckedGroups();
        }

        /// <summary>
        /// Выбрать по Дате (в группах с этими папками).
        /// </summary>
        /// <param name="Индекс выделенного файла"></param>
        public void CheckByDate(int index, SortByDateEnum dateSort)
        {
            List<string> listOfPathsForCompare = GetListOfThisFolder(Items[index].Group);
            UpdateListOfGroups();
            //проверяем каждую группу на наличие ВСЕХ записей из искомой
            foreach (GroupOfDupl group in _groups)
            {
                bool check = true;
                foreach (string compared in listOfPathsForCompare) //в искомой
                {
                    bool groupApproaches = false;
                    foreach (ListViewItemSave item in group.Items) //в текущей
                    {
                        if (String.Compare(compared, item.Directory) == 0) //path equal
                        {
                            groupApproaches = true;
                            break;
                        }
                    }
                    //если не найдена то пропускаем
                    if (!groupApproaches)
                    {
                        check = false; //группа не подходит
                        break; //выход из общего цикла
                    }

                }
                if (check) //помечаем
                {
                    CheckGroupByDate(group, dateSort);
                }
            }
        }

        public void CheckByDate(ListView.SelectedIndexCollection selectedIndexCollection, SortByDateEnum dateSort)
        {
            DateTime compareDate;
            DateTime currentDate;
            List<string> groupsHash = new List<string>(selectedIndexCollection.Count);
            for (int j = 0; j < selectedIndexCollection.Count; j++)
            {
                if (!groupsHash.Contains(Items[selectedIndexCollection[j]].Group))
                    groupsHash.Add(Items[selectedIndexCollection[j]].Group);
            }

            FormProgress formProgress = new FormProgress();
            Dictionary<string, List<ListViewItemSave>> groups = GetDictionaryOfGroups();
            formProgress.Show();
            formProgress.SetProgressMax(groups.Count);
            foreach (string hash in groupsHash)
            {
                int indexForUnchecked = 0;
                compareDate = groups[hash][0].DateTime;
                for (int i = 0; i < groups[hash].Count; i++)
                {
                    groups[hash][i].Checked = true;
                    currentDate = groups[hash][i].DateTime;
                    if (dateSort == SortByDateEnum.OlderFirst)
                    {
                        if (DateTime.Compare(currentDate, compareDate) > 0)
                        {
                            compareDate = currentDate;
                            indexForUnchecked = i;
                        }
                    }
                    else if (dateSort == SortByDateEnum.NewestFirst)
                    {
                        if (DateTime.Compare(currentDate, compareDate) < 0)
                        {
                            compareDate = currentDate;
                            indexForUnchecked = i;
                        }
                    }
                    formProgress.PerformStepEventHandler();
                }
                groups[hash][indexForUnchecked].Checked = false;
            }
            formProgress.Close();
            FillItemsFromDictionary(groups);
        }

        public void CheckByDate(SortByDateEnum dateSort)
        {
            UpdateListOfGroups();
            foreach (GroupOfDupl group in _groups)
            {
                CheckGroupByDate(group, dateSort);
            }
        }

        private void CheckGroupByDate(GroupOfDupl group, SortByDateEnum dateSort)
        {
            DateTime compareDate;
            DateTime currentDate;
            compareDate = group.Items[0].DateTime;
            group.Items[0].Checked = true;
            int indexUnchecked = 0;
            for (int i = 1; i < group.Items.Count; i++)
            {
                group.Items[i].Checked = true;
                currentDate = group.Items[i].DateTime;
                if (dateSort == SortByDateEnum.OlderFirst)
                {
                    if (DateTime.Compare(currentDate, compareDate) > 0)
                    {
                        compareDate = currentDate;
                        indexUnchecked = i;
                    }
                }
                else if (dateSort == SortByDateEnum.NewestFirst)
                {
                    if (DateTime.Compare(currentDate, compareDate) < 0)
                    {
                        compareDate = currentDate;
                        indexUnchecked = i;
                    }
                }
            }
            group.Items[indexUnchecked].Checked = false;
        }

        public void CheckAllButOne()
        {
            string prevCheckSum = String.Empty;
            string curCheckSum = String.Empty;
            FormProgress formProgress = new FormProgress();
            formProgress.Show();
            formProgress.SetProgressMax(Items.Count);
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].Checked = false;
                curCheckSum = Items[i].SubItems[5].Text;
                if (prevCheckSum != null)
                    if (prevCheckSum == curCheckSum)
                        Items[i].Checked = true;
                prevCheckSum = curCheckSum;
                formProgress.PerformStepEventHandler();
            }
            formProgress.Close();
        }

        /// <summary>
        /// Выделить все кроме одного в выделенных.
        /// </summary>
        public void CheckAllButOne(ListView.SelectedIndexCollection selectedIndexCollection)
        {
            string prevCheckSum = String.Empty;
            string curCheckSum = String.Empty;
            FormProgress formProgress = new FormProgress();
            formProgress.Show();
            formProgress.SetProgressMax(selectedIndexCollection.Count);
            for (int i = 0; i < selectedIndexCollection.Count; i++)
            {
                Items[selectedIndexCollection[i]].Checked = false;
                curCheckSum = Items[selectedIndexCollection[i]].CheckSum;
                if (prevCheckSum != String.Empty)
                    if (prevCheckSum == curCheckSum)
                        Items[selectedIndexCollection[i]].Checked = true;
                prevCheckSum = curCheckSum;
                formProgress.PerformStepEventHandler();
            }
            formProgress.Close();
        }

        public void CheckAllInThisFolder(int index)
        {
            string path = GetDirectory(index);
            foreach (ListViewItemSave item in Items)
            {
                if (item.Directory == path)
                    item.Checked = true;
            }

            ColoringAllCheckedGroups();
        }

        public void CheckAll(ListView.SelectedIndexCollection selectedIndexCollection)
        {
            for (int i = 0; i < selectedIndexCollection.Count; i++)
            {
                Items[selectedIndexCollection[i]].Checked = true;
            }
        }

        public void CheckAll()
        {
            foreach (ListViewItemSave item in Items)
            {
                item.Checked = true;
            }

            //ColoringAllCheckedGroups();
        }

        public void CheckByName(ListView.SelectedIndexCollection selectedIndexCollection, string nameForSelect)
        {
            for (int i = 0; i < selectedIndexCollection.Count; i++)
            {
                if (Items[selectedIndexCollection[i]].FileName.ToLower().Contains(nameForSelect))
                    Items[selectedIndexCollection[i]].Checked = true;
            }

            ColoringAllCheckedGroups();
        }

        public void CheckByName(string nameForSelect)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].FileName.ToLower().Contains(nameForSelect))
                    Items[i].Checked = true;
            }

            ColoringAllCheckedGroups();
        }

        public void CheckByFileNameLength(ListView.SelectedIndexCollection selectedIndexCollection, SortByFileNameLengthEnum fileNameLengthSort)
        {
            string extremeName;
            string currentName;

            List<string> groupsHash = new List<string>(selectedIndexCollection.Count);
            for (int j = 0; j < selectedIndexCollection.Count; j++)
            {
                if (!groupsHash.Contains(Items[selectedIndexCollection[j]].Group))
                    groupsHash.Add(Items[selectedIndexCollection[j]].Group);
            }

            Dictionary<string, List<ListViewItemSave>> groups = GetDictionaryOfGroups();

            foreach (string hash in groupsHash)
            {
                int indexForUnchecked = 0;
                extremeName = groups[hash][0].FileName;
                for (int i = 0; i < groups[hash].Count; i++)
                {
                    groups[hash][i].Checked = true;
                    currentName = groups[hash][i].FileName;
                    if (fileNameLengthSort == SortByFileNameLengthEnum.ShorterFirst)
                    {
                        if (extremeName.Length < currentName.Length)
                        {
                            extremeName = currentName;
                            indexForUnchecked = i;
                        }
                    }
                    else if (fileNameLengthSort == SortByFileNameLengthEnum.LongerFirst)
                    {
                        if (extremeName.Length > currentName.Length)
                        {
                            extremeName = currentName;
                            indexForUnchecked = i;
                        }
                    }
                }
                groups[hash][indexForUnchecked].Checked = false;
            }

            FillItemsFromDictionary(groups);
        }

        public void CheckByFileNameLength(SortByFileNameLengthEnum sortByFileNameLengthEnum)
        {
            string extremeName;
            string currentName;

            UpdateListOfGroups();

            foreach (GroupOfDupl group in _groups)
            {
                int indexForUnchecked = 0;
                extremeName = group.Items[0].FileName;
                for (int i = 0; i < group.Items.Count; i++)
                {
                    group.Items[i].Checked = true;
                    currentName = group.Items[i].FileName;
                    if (sortByFileNameLengthEnum == SortByFileNameLengthEnum.ShorterFirst)
                    {
                        if (extremeName.Length < currentName.Length)
                        {
                            extremeName = currentName;
                            indexForUnchecked = i;
                        }
                    }
                    else if (sortByFileNameLengthEnum == SortByFileNameLengthEnum.LongerFirst)
                    {
                        if (extremeName.Length > currentName.Length)
                        {
                            extremeName = currentName;
                            indexForUnchecked = i;
                        }
                    }
                }
                group.Items[indexForUnchecked].Checked = false;
            }
        }


        public void CheckByNumberInFileName(SortByNumberInFileNameEnum sortByNumberInFileNameEnum)
        {
            UpdateListOfGroups();

            foreach (GroupOfDupl group in _groups)
            {
                int extremeNumber;
                int currentNumber;
                int indexForUnchecked = 0;

                if (!CheckAllWithNumber(group.Items))
                    continue;

                extremeNumber = GetNumber(group.Items[0].FileName);

                for (int i = 0; i < group.Items.Count; i++)
                {
                    group.Items[i].Checked = true;
                    currentNumber = GetNumber(group.Items[i].FileName);
                    if (sortByNumberInFileNameEnum == SortByNumberInFileNameEnum.BiggerFirst)
                    {
                        if (extremeNumber < currentNumber)
                        {
                            extremeNumber = currentNumber;
                            indexForUnchecked = i;
                        }
                    }
                    else if (sortByNumberInFileNameEnum == SortByNumberInFileNameEnum.LowestFirst)
                    {
                        if (extremeNumber > currentNumber)
                        {
                            extremeNumber = currentNumber;
                            indexForUnchecked = i;
                        }
                    }
                }
                group.Items[indexForUnchecked].Checked = false;
            }
        }

        private bool CheckAllWithNumber(List<ListViewItemSave> items)
        {
            bool allWithNumber = true;
            foreach (var item in items)
            {
                int number = GetNumber(item.FileName);
                if (number == -1)
                    return false;
            }
            return allWithNumber;
        }

        private int GetNumber(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            int index = fileName.IndexOf("_");
            if (index == -1)
                return -1;

            int extremeNumber;
            if (Int32.TryParse(fileName.Substring(0, index), out extremeNumber))
            {
                return extremeNumber;
            }
            return -1;
        }


        #endregion

        public void Sort(ListViewSaveGroupSorter sorter)
        {
            UpdateListOfGroups(sorter.ListViewItemSorter);
            _groups.Sort(sorter);
            FillItemsFromList();
        }

        /// <summary>
        /// Обновляем список _groups из Items.
        /// </summary>
        private void UpdateListOfGroups()
        {
            UpdateListOfGroups(null);
        }

        private void UpdateListOfGroups(ListViewItemSaveSorter itemSorter)
        {
            if (_groups == null)
                _groups = new List<GroupOfDupl>();
            else
                _groups.Clear();
            //List<GroupOfDupl> _groups = new List<GroupOfDupl>();
            //заполняем группы файлов
            GroupOfDupl group = new GroupOfDupl();
            string lastHash = String.Empty;
            foreach (ListViewItemSave item in Items)
            {
                if (item.Group != lastHash) //new group
                {
                    //добавляем предыдущую группу
                    //if (group != null)
                    if (!String.IsNullOrEmpty(group.Name))
                    {
                        if (itemSorter != null)
                            group.Items.Sort(itemSorter);
                        _groups.Add(group);
                    }

                    //создаем новую группу
                    //if (group == null)
                    group = new GroupOfDupl(item.Group);
                    //group.Name = item.GetGroup;
                    group.Items.Add(item);
                }
                else //exist group
                {
                    //добавляем элементы в текущую группу
                    //if (group == null)
                    //group = new GroupOfDupl(item.GetGroup);
                    group.Items.Add(item);
                }

                lastHash = item.Group;
            }
            _groups.Add(group);
        }

        private void FillItemsFromList()
        {
            //удаляем и вставляем элементы в соответсвиие с тем как они отсортированы в группах
            Items.Clear();
            foreach (GroupOfDupl group in _groups)
            {
                foreach (ListViewItemSave item in group.Items)
                    Items.Add(item);
            }

            //if (coloringEnabled)
            ColoringOfGroups();
        }

        /// <summary>
        /// Установка цветов для Items
        /// </summary>
        public void ColoringOfGroups()
        {
            Settings settings = Settings.GetInstance();
            bool prevIsColored = false;
            string lastHash = String.Empty;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Group != lastHash)
                {
                    //новый цвет
                    if (prevIsColored)
                        prevIsColored = false;
                    else
                        prevIsColored = true;
                    lastHash = Items[i].SubItems[5].Text;
                }
                if (prevIsColored)
                {
                    if (Items[i].Color != settings.Fields.ColorRowError &&
                            Items[i].Color != settings.Fields.ColorRowNotExist)
                        //if (Items[i].Color != settings.Fields.ColorRowNotExist)
                        Items[i].Color = settings.Fields.ColorRow1;
                }
                else
                {
                    if (Items[i].Color != settings.Fields.ColorRowError &&
                            Items[i].Color != settings.Fields.ColorRowNotExist)
                        //if (Items[i].Color != settings.Fields.ColorRowNotExist)
                        Items[i].Color = settings.Fields.ColorRow2;
                }
            }
        }

        /// <summary>
        /// Проверка есть ли все отмеченные группах.
        /// </summary>
        /// <param name="indexOfGroupWithAllChecked"></param>
        /// <returns></returns>
        public bool CheckAllChekedInGroup(out int indexOfGroupWithAllChecked)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (AllChekedInGroups(Items[i].Group))
                {
                    indexOfGroupWithAllChecked = i;
                    return true;
                }
            }
            indexOfGroupWithAllChecked = 0;
            return false;
        }

        private bool AllChekedInGroups(string group)
        {
            Boolean allChecked = true;
            List<ListViewItemSave> groupList = new List<ListViewItemSave>();
            foreach (ListViewItemSave item in Items)
            {
                if (item.Group == group)
                {
                    groupList.Add(item);
                    if (!item.Checked)
                    {
                        allChecked = false;
                        break;
                    }
                }
            }
            if (allChecked)
            {
                foreach (ListViewItemSave item in groupList)
                {
                    item.Color = Settings.GetInstance().Fields.ColorRowError;
                }
            }
            else
            {
                foreach (ListViewItemSave item in groupList)
                {
                    item.Color = Settings.GetInstance().Fields.ColorRow1;
                }
            }
            return allChecked;
        }

        /// <summary>
        /// Удаление выделенных.
        /// </summary>
        public void DeleteChekedItems()
        {
            List<ListViewItemSave> checkedItems = GetCheckedList();
            int count = checkedItems.Count();

            // Create dialog.
            FormProgressWithBackgroundWorker formProgress = new FormProgressWithBackgroundWorker("Deleting cheked items", count,
                (object sender, DoWorkEventArgs e) =>
                //delegate(object sender, DoWorkEventArgs e)
                {
                    BackgroundWorker worker = sender as BackgroundWorker;
                    foreach (ListViewItemSave item in checkedItems)
                    {
                        if (worker.CancellationPending) // See if cacel button was pressed.
                            break;
                        string path = Path.Combine(item.Directory, item.FileName);
                        if (File.Exists(path))
                        {
                            try
                            {
                                FileUtils.MoveToRecycleBin(path);
                                Items.Remove(item);
                            }
                            catch (System.UnauthorizedAccessException ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }
                        else //not exist
                        {
                            //Пометка серым отсутствующих
                            item.Color = Settings.GetInstance().Fields.ColorRowNotExist;
                        }
                        worker.ReportProgress(0, null); // Report percent and user-status info to dialog.
                    }

                    worker.ReportProgress(0, "Delete Empty Groups");
                    DeleteEmptyGroups();
                });
            // Show dialog with Synchronous/blocking call.
            // LongOperation() is called by dialog.
            formProgress.ShowDialog(); // Synchronous/blocking call.
        }

        /// <summary>
        /// Перенести выделенные файлы по переданному пути.
        /// </summary>
        public void MoveCheckedToFolder(string selectedPath)
        {
            List<ListViewItemSave> checkedItems = GetCheckedList();

            FormProgressWithBackgroundWorker formProgress = new FormProgressWithBackgroundWorker("Move checked items to folder", checkedItems.Count,
                (object sender, DoWorkEventArgs e) =>
                {
                    BackgroundWorker worker = sender as BackgroundWorker;
                    foreach (ListViewItemSave item in checkedItems)
                    {
                        string sourceFolder = item.Directory;
                        string sourcePath = Path.Combine(sourceFolder, item.FileName);
                        if (File.Exists(sourcePath))
                        {
                            if (!Directory.Exists(selectedPath))
                                Directory.CreateDirectory(selectedPath);

                            string targetPath = Path.Combine(selectedPath, item.FileName);
                            try
                            {
                                if (File.Exists(targetPath))
                                    targetPath = Rename.SimilarRename(targetPath, sourcePath);
                                new FileInfo(sourcePath).MoveTo(targetPath);
                                item.Directory = Path.GetDirectoryName(targetPath);
                                item.FileName = Path.GetFileName(targetPath);

                                /*if (IsSearchDirectoryContain(selectedPath, searchDirectory))
                                {
                                    //Edit Label Path
                                    lvi.Text = Path.GetFileName(targetPath);
                                    lvi.SubItems["Path"].Text = Directory.GetParent(targetPath).ToString();
                                }
                                else
                                {
                                    //Delete Item
                                    DeleteEmptyGroups();
                                }*/
                            }
                            catch (System.UnauthorizedAccessException ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }
                        worker.ReportProgress(0, null); // Report percent and user-status info to dialog.
                    }
                });
            formProgress.ShowDialog();
        }

        /// <summary>
        /// Есть ли переданный путь в путях для поиска дубликатов.
        /// </summary>
        /*private bool IsSearchDirectoryContain(string movePath, string[] searchDirectory)
        {
            bool ContainSearchPath = false;
            char[] split = new char[] { Path.DirectorySeparatorChar };
            for (int i = 0; i < lvDirectorySearch.CheckedItems.Count; i++)
            {
                string[] search = lvDirectorySearch.CheckedItems[i].ToString().Split(split, StringSplitOptions.RemoveEmptyEntries);
                string[] move = movePath.Split(split, StringSplitOptions.RemoveEmptyEntries);

                bool Include = true;
                if (move.Length < search.Length)
                {
                    Include = false;
                }
                else
                {
                    for (int j = 0; j < search.Length; j++)
                    {
                        if (!String.Equals(search[j], move[j]))
                        {
                            //IsContainSearchPath = true;
                            Include = false;
                            break;
                        }
                    }
                }

                if (Include)
                {
                    ContainSearchPath = true;
                    break;
                }
            }
            return ContainSearchPath;
        }*/

        /// <summary>
        /// Удаляет группы с количеством элементов меньше двух.
        /// </summary>
        private void DeleteEmptyGroups()
        {
            UpdateListOfGroups();
            _groups.RemoveAll(delegate(GroupOfDupl group)
            {
                return (group.Items.Count < 2);
            });
            FillItemsFromList();
        }

        public bool DeleteItem(int index)
        {
            string path = GetPath(index);
            if (System.IO.File.Exists(path))
                try
                {
                    FileUtils.MoveToRecycleBin(path);
                    Items.RemoveAt(index);
                    DeleteEmptyGroups();
                    return true;
                }
                catch (System.UnauthorizedAccessException ex)
                {
                    MessageBox.Show(ex.Message);
                    return false;
                }
            return false;
        }

        public void FileNotExist(int index)
        {
            /*string group = Items[indexOfGroupWithAllChecked].GetGroup;
            foreach (ListViewItemSave item in Items)
            {
                if (item.GetGroup == group)
                {
                    string path = Path.Combine(item.Directory, item.FileName);
                    if (!File.Exists(path))
                    {
                        item.Color = Settings.GetInstance().Fields.ColorRowNotExist;
                    }
                }
            }*/
            //все группы
            foreach (ListViewItemSave item in Items)
            {
                if (!File.Exists(Path.Combine(item.Directory, item.FileName)))
                    item.Color = Settings.GetInstance().Fields.ColorRowNotExist;
            }
        }

        /// <summary>
        /// Возврашает группу этого файла.
        /// </summary>
        public GroupOfDupl GetGroup(int index)
        {
            string groupHash = Items[index].Group;
            GroupOfDupl group = new GroupOfDupl(groupHash);
            foreach (ListViewItemSave item in Items)
            {
                if (item.Group == groupHash)
                    group.Items.Add(item);
            }
            return group;
        }

        public void DeleteGroupsFromList(ListView.SelectedIndexCollection selectedIndex)
        {
            List<string> groupsHash = new List<string>(selectedIndex.Count);
            for (int j = 0; j < selectedIndex.Count; j++)
            {
                if (!groupsHash.Contains(Items[selectedIndex[j]].Group))
                    groupsHash.Add(Items[selectedIndex[j]].Group);
            }

            Dictionary<string, List<ListViewItemSave>> groups = GetDictionaryOfGroups();
            foreach (string hash in groupsHash)
            {
                groups.Remove(hash);
            }
            FillItemsFromDictionary(groups);
        }

        public bool DeleteGroupFromList(int index)
        {
            string groupHash = Items[index].Group;

            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Group == groupHash)
                {
                    Items.RemoveAll(delegate(ListViewItemSave item)
                    {
                        return item.Group == groupHash;
                    });
                }
            }
            ColoringOfGroups();
            return true;
        }

        public bool RemoveMissingFilesFromList()
        {
            int deleted = 0;

            // Create dialog.
            FormProgressWithBackgroundWorker formProgress = new FormProgressWithBackgroundWorker("Remove missing files from list", Items.Count,
                delegate(object sender, DoWorkEventArgs e)
                {
                    BackgroundWorker worker = sender as BackgroundWorker;

                    deleted = Items.RemoveAll(delegate(ListViewItemSave item)
                    {
                        string path = Path.Combine(item.Directory, item.FileName);
                        worker.ReportProgress(0, null); // Report percent and user-status info to dialog.
                        return !File.Exists(path);
                    });
                });
            formProgress.ShowDialog(); // Synchronous/blocking call.

            if (deleted > 0)
            {
                DeleteEmptyGroups();
                return true;
            }
            return false;
        }

        private bool AllChekedInGroup(GroupOfDupl group)
        {
            foreach (ListViewItemSave item in group.Items)
            {
                if (!item.Checked)
                    return false;
            }
            return true;
        }

        public bool AllChekedInGroup(int index)
        {
            bool allChecked = true;
            string groupHash = Items[index].Group;
            if (!Settings.GetInstance().Fields.IsAllowDelAllFiles)
            {
                UpdateListOfGroups();

                foreach (GroupOfDupl group in _groups)
                {
                    if (group.Name == groupHash)
                    {
                        foreach (ListViewItemSave item in group.Items)
                        {
                            if (!item.Checked)
                                allChecked = false;
                        }
                        if (allChecked)
                        {
                            foreach (ListViewItemSave item in group.Items)
                                item.Color = Settings.GetInstance().Fields.ColorRowError;

                            FillItemsFromList();
                        }
                        break;
                    }
                }
            }
            return allChecked;
        }

        public bool ColoringAllCheckedGroups()
        {
            bool allCheckedExist = false;
            if (!Settings.GetInstance().Fields.IsAllowDelAllFiles)
            {
                UpdateListOfGroups();

                foreach (GroupOfDupl group in _groups)
                {
                    if (AllChekedInGroup(group))
                    {
                        allCheckedExist = true;
                        foreach (ListViewItemSave item in group.Items)
                        {
                            item.Color = Settings.GetInstance().Fields.ColorRowError;
                        }
                    }
                    else
                    {
                        foreach (ListViewItemSave item in group.Items)
                        {
                            item.Color = Settings.GetInstance().Fields.ColorRow1;
                        }
                    }
                }

                FillItemsFromList();
            }
            return allCheckedExist;
        }

        public void MoveFileToNeighbour(int index)
        {
            string sourceDir = GetDirectory(index);
            string sourceName = GetFileName(index);
            string sourcePath = Path.Combine(sourceDir, sourceName);
            string targetPath = null;
            string targetDir = null;
            GroupOfDupl group = GetGroup(index);
            string item1 = group.Items[0].Directory;
            string item2 = group.Items[1].Directory;
            if (String.Compare(sourceDir, item1, true) == 0)
                targetDir = item2;
            else
                targetDir = item1;
            targetPath = Path.Combine(targetDir, sourceName);

            if (File.Exists(sourcePath))
            {
                try
                {
                    if (File.Exists(targetPath))
                        targetPath = Rename.SimilarRename(targetPath, sourcePath);

                    new FileInfo(sourcePath).MoveTo(targetPath);

                    Items[index].Directory = targetDir;
                    Items[index].FileName = Path.GetFileName(targetPath);
                }
                catch (System.UnauthorizedAccessException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        /// <summary>
        /// Переименовать в текущей директории.
        /// </summary>
        /// <param name="indexOfGroupWithAllChecked"></param>
        /// <param name="newFileName"></param>
        /// <returns></returns>
        public bool RenameTo(int index, string newFileName)
        {
            if (File.Exists(newFileName))
                return false;

            string name = GetFileName(index);
            if (String.Compare(newFileName, name) != 0) //если не равны
            {
                string sourcePath = GetPath(index);
                string destFileName = Path.Combine(GetDirectory(index), newFileName);
                if (File.Exists(sourcePath))
                {
                    try
                    {
                        new FileInfo(sourcePath).MoveTo(destFileName);
                        Items[index].FileName = newFileName;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return false;
                    }
                }
            }
            return false;
        }

        public bool RenameLikeNeighbour(int index)
        {
            string sourceName = GetFileName(index);
            string sourceFolder = GetDirectory(index);
            string sourcePath = Path.Combine(sourceFolder, sourceName);
            string targetName = null;
            string targetPath = null;

            if (!File.Exists(sourcePath))
                return false;

            GroupOfDupl group = GetGroup(index);
            string item1 = group.Items[0].FileName;
            string item2 = group.Items[1].FileName;
            if (String.Compare(sourceName, item1, true) == 0)
                targetName = item2;
            else
                targetName = item1;
            targetPath = Path.Combine(sourceFolder, targetName);

            try
            {
                if (File.Exists(targetPath))
                    targetPath = Rename.SimilarRename(targetPath, sourcePath);
                new FileInfo(sourcePath).MoveTo(targetPath);
                //Update Text
                Items[index].FileName = Path.GetFileName(targetPath);
                return true;
            }
            catch (System.UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public void DeselectAll()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].Checked = false;
            }
        }

        public void DeselectAll(ListView.SelectedIndexCollection selectedIndexCollection)
        {
            List<string> groupsHash = new List<string>(selectedIndexCollection.Count);
            for (int j = 0; j < selectedIndexCollection.Count; j++)
            {
                if (!groupsHash.Contains(Items[selectedIndexCollection[j]].Group))
                    groupsHash.Add(Items[selectedIndexCollection[j]].Group);
            }

            Dictionary<string, List<ListViewItemSave>> groups = GetDictionaryOfGroups();
            foreach (string hash in groupsHash)
            {
                foreach (ListViewItemSave item in groups[hash])
                    item.Checked = false;
            }
            FillItemsFromDictionary(groups);
        }

        public void ClearColorForGroup(int index)
        {
            string groupHash = Items[index].Group;
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Group == groupHash)
                {
                    Items[i].Color = Settings.GetInstance().Fields.ColorRow1;
                }
            }
        }


    }
}
