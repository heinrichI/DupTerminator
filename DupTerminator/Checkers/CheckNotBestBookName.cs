using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace DupTerminator.Checkers
{
    public class CheckNotBestBookName
    {
        enum GroupType
        {
            None,
            OneHaveYear,
            AllHaveYear,
            OneHaveYearAndOneHaveWriter,
            OneHaveYearAndAllHaveWriter,
            AllHaveYearAndOneHaveWriter,
            AllHaveYearAndAllHaveWriter,
            OneHaveWriter,
            AllHaveWriter
        }

        static string[] _keepPrefix =
        {
            "МО_",
            "ВРАЖЕСКАЯ",
            "ВРАЖЕСКОЕ"
        };

        internal static void Select(UndoRedoEngine undoRedoEngine, System.Windows.Forms.ListView.SelectedIndexCollection selectedIndices)
        {
            if (selectedIndices.Count > 1) //в выбранных
            {
                var groups = undoRedoEngine.ListDuplicates.GetGroups(selectedIndices);
                //если в одной есть год а в другой есть автор, то переименовать обе
                foreach (GroupOfDupl group in groups)
                {
                    HandleGroup(group, undoRedoEngine);
                }
            }
            else
            {
                undoRedoEngine.ListDuplicates.UpdateListOfGroups();
                foreach (GroupOfDupl group in undoRedoEngine.ListDuplicates._groups)
                {
                    HandleGroup(group, undoRedoEngine);
                }
            }       
        }

        private static void HandleGroup(GroupOfDupl group, UndoRedoEngine undoRedoEngine)
        {
            var groupType = GetGroupType(group);
            bool yearEqual;
            int year;
            switch (groupType)
            {
                case GroupType.None:
                    if (OneOnCyrilikAnotherTranslit(group))
                    {
                        for (int i = 0; i < group.Items.Count; i++)
                        {
                            if (OnlyLatin(Path.GetFileNameWithoutExtension(group.Items[i].FileName)))
                            {
                                Check(group.Items[i], group);
                            }
                        }
                        break;
                    }
                    if (EqualAndEndWith12(group))
                    {
                        for (int i = 0; i < group.Items.Count; i++)
                        {
                            if (EndWith12(Path.GetFileNameWithoutExtension(group.Items[i].FileName)))
                            {
                                Check(group.Items[i], group);
                            }
                        }
                        break;
                    }
                    if (EqualExceptKeepPrefiks(group))
                    {
                        for (int i = 0; i < group.Items.Count; i++)
                        {
                            if (!StartFrompPrefiks(Path.GetFileNameWithoutExtension(group.Items[i].FileName)))
                            {
                                group.Items[i].Checked = true;
                            }
                        }
                    }
                    break;
                case GroupType.OneHaveYear:
                    bool itemChecked = false;
                    for (int i = 0; i < group.Items.Count; i++)
                    {
                        if (!EndWithYear(Path.GetFileNameWithoutExtension(group.Items[i].FileName), out _))
                        {
                            if (Check(group.Items[i], group))
                                itemChecked = true;
                        }
                    }
                    if (!itemChecked)
                    {
                        (yearEqual, year) = YearsEqual(group);
                        for (int i = 0; i < group.Items.Count; i++)
                        {
                            if (!EndWithYear(Path.GetFileNameWithoutExtension(group.Items[i].FileName), out _))
                            {
                                string newName = Path.GetFileNameWithoutExtension(group.Items[i].FileName) + "_" + year + Path.GetExtension(group.Items[i].FileName);
                                if (MessageBox.Show($"Rename {group.Items[i].FileName} to {newName}", "Rename?",
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                {
                                    int index = undoRedoEngine.ListDuplicates.Items.IndexOf(group.Items[i]);
                                    undoRedoEngine.RenameTo(index, newName);
                                }
                            }
                        }
                    }
                    break;
                case GroupType.AllHaveYear:
                    if (OneOnCyrilikAnotherTranslit(group))
                        for (int i = 0; i < group.Items.Count; i++)
                        {
                            if (OnlyLatin(Path.GetFileNameWithoutExtension(group.Items[i].FileName)))
                            {
                                Check(group.Items[i], group);
                            }
                        }
                    break;
                case GroupType.OneHaveYearAndOneHaveWriter:
                    (yearEqual, year) = YearsEqual(group);
                    for (int i = 0; i < group.Items.Count; i++)
                    {
                        if (!EndWithYear(Path.GetFileNameWithoutExtension(group.Items[i].FileName), out _))
                        {
                            // есть в другом файле
                            if (HaveWriter(group.Items.Except(new[] { group.Items[i] })))
                                Check(group.Items[i], group);
                            else if (yearEqual)
                            {
                                string newName = Path.GetFileNameWithoutExtension(group.Items[i].FileName) + "_" + year + Path.GetExtension(group.Items[i].FileName);
                                if (MessageBox.Show($"Rename {group.Items[i].FileName} to {newName}", "Rename?",
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                {
                                    int index = undoRedoEngine.ListDuplicates.Items.IndexOf(group.Items[i]);
                                    undoRedoEngine.RenameTo(index, newName);
                                }
                            }
                        }
                    }
                    break;
                case GroupType.OneHaveYearAndAllHaveWriter:
                    for (int i = 0; i < group.Items.Count; i++)
                    {
                        if (!EndWithYear(Path.GetFileNameWithoutExtension(group.Items[i].FileName), out _))
                        {
                             Check(group.Items[i], group);
                        }
                    }
                    break;
                case GroupType.AllHaveYearAndOneHaveWriter:
                    for (int i = 0; i < group.Items.Count; i++)
                    {
                        if (!StartFromWriter(Path.GetFileNameWithoutExtension(group.Items[i].FileName)))
                            Check(group.Items[i], group);
                    }
                    break;
                case GroupType.AllHaveYearAndAllHaveWriter:
                    //если начало и конец совпадает, то удалаем более короткий
                    if (StartAndEndEqual(group))
                    {
                        int maxLen = group.Items.Max(d => d.FileName.Length);
                        for (int i = 0; i < group.Items.Count; i++)
                        {
                            if (group.Items[i].FileName.Length < maxLen)
                                Check(group.Items[i], group);
                        }
                    }
                    break;
                case GroupType.OneHaveWriter:
                    for (int i = 0; i < group.Items.Count; i++)
                    {
                        if (!StartFromWriter(Path.GetFileNameWithoutExtension(group.Items[i].FileName)))
                            Check(group.Items[i], group);
                    }
                    break;
                case GroupType.AllHaveWriter:
                    if (EqualAndEndWith12(group))
                    {
                        for (int i = 0; i < group.Items.Count; i++)
                        {
                            if (EndWith12(Path.GetFileNameWithoutExtension(group.Items[i].FileName)))
                            {
                                Check(group.Items[i], group);
                            }
                        }
                        break;
                    }
                    break;
                default:
                    break;
            }
        
        }

        private static (bool, int) YearsEqual(GroupOfDupl group)
        {
            List<int> years = new List<int>();
            for (int i = 0; i < group.Items.Count; i++)
            {
                if (EndWithYear(Path.GetFileNameWithoutExtension(group.Items[i].FileName), out int? year))
                    years.Add(year.Value);
            }
            return (years.All(y => y == years.First()), years.First());
        }

        private static bool StartFrompPrefiks(string name)
        {
            foreach (string prefix in _keepPrefix)
            {
                if (name.StartsWith(prefix))
                    return true;
            }
            return false;
        }

        private static bool EqualExceptKeepPrefiks(GroupOfDupl group)
        {
            foreach (string prefix in _keepPrefix)
            {
                var names = group.Items.Select(i =>
                {
                    int index = i.FileName.IndexOf(prefix);
                    if (index != -1)
                    {
                        return i.FileName.Substring(index);
                    }
                    else
                        return i.FileName;
                });
                string first = names.First();
                if (names.All(n => n == first))
                    return true;
            }
            return false;
        }

        private static bool EndWith12(string name)
        {
            return name.EndsWith("(1)") 
                || name.EndsWith("(2)")
                || name.EndsWith("_");
        }

        private static bool EqualAndEndWith12(GroupOfDupl group)
        {
            int haveNumberCount = 0;
            List<string> names = new List<string>();
            foreach (var item in group.Items)
            {
                string name = Path.GetFileNameWithoutExtension(item.FileName);
                if (EndWith12(name))
                {
                    haveNumberCount++;
                    string cut = name.Substring(0, name.Length - 3).TrimEnd();
                    names.Add(cut);
                }
                else
                {
                    names.Add(name);
                }
            }
            bool equal = names.Count >= 2 && names.All(n => n == names.First());
            return haveNumberCount > 0 && haveNumberCount < group.Items.Count && equal;
        }

        private static bool StartAndEndEqual(GroupOfDupl group)
        {
            string start = GetSubstringUpToThirdUnderscore(group.Items.First().FileName);
            bool startEqual = group.Items.All(i => i.FileName.StartsWith(start));
            string fileName = Path.GetFileNameWithoutExtension(group.Items.First().FileName);
            string end = fileName.Substring(fileName.Length - 4);
            bool endEqual = group.Items.All(i => Path.GetFileNameWithoutExtension(i.FileName).EndsWith(end));
            return startEqual && endEqual;
        }

        static string GetSubstringUpToThirdUnderscore(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            int underscoreCount = 0;
            int index = 0;

            // Iterate through the string to find the third underscore
            while (index < input.Length)
            {
                if (input[index] == '_')
                {
                    underscoreCount++;
                    if (underscoreCount == 3)
                    {
                        return input.Substring(0, index);
                    }
                }
                index++;
            }

            // If there are less than three underscores, return the entire string
            return input;
        }

        private static bool OneOnCyrilikAnotherTranslit(GroupOfDupl group)
        {
            bool haveCyrilic = false;
            bool haveLatin = false;
            foreach (var item in group.Items)
            {
                if (MostCyrilic(Path.GetFileNameWithoutExtension(item.FileName)))
                    haveCyrilic = true; 
                if (OnlyLatin(Path.GetFileNameWithoutExtension(item.FileName)))
                    haveLatin = true;
            }
            return haveLatin && haveCyrilic;
        }

        public static bool OnlyLatin(string name)
        {
            string pattern = @"^[a-zA-Z0-9_ .\-\[\]]*$";
            return Regex.IsMatch(name, pattern, RegexOptions.Compiled);
        }

        public static bool MostCyrilic(string name)
        {
            //string pattern = @"^[p{Cyrillic}ds_]+$";
            (int latinCount, int cyrillicCount) = CountSymbols(name);
            if (cyrillicCount > 1 && latinCount <= 5)
                return true;
            return false;
        }

        static (int, int) CountSymbols(string input)
        {
            int latinCount = 0;
            int cyrillicCount = 0;

            foreach (char c in input)
            {
                if (char.IsLetter(c))
                {
                    if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                    {
                        latinCount++;
                    }
                    else if ((c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я'))
                    {
                        cyrillicCount++;
                    }
                }
            }

            return (latinCount, cyrillicCount);
        }

        private static GroupType GetGroupType(GroupOfDupl group)
        {
            bool haveYear = false;
            bool haveWriter = false;
            bool notAllHaveYear = false;
            bool notAllHaveWriter = false;
            foreach (var item in group.Items)
            {
                string name = Path.GetFileNameWithoutExtension(item.FileName);
                if (EndWithYear(name, out _))
                    haveYear = true;
                else
                    notAllHaveYear = true;
                if (StartFromWriter(name))
                    haveWriter = true;
                else
                    notAllHaveWriter = true;
            }

            if (haveYear && !haveWriter)
                return notAllHaveYear ? GroupType.OneHaveYear : GroupType.AllHaveYear;
            if (!haveYear && haveWriter)
                return notAllHaveWriter ? GroupType.OneHaveWriter : GroupType.AllHaveWriter;
            if (haveYear && haveWriter)
            {
                if (notAllHaveYear)
                {
                    if (notAllHaveWriter)
                        return GroupType.OneHaveYearAndOneHaveWriter;
                    else
                        return GroupType.OneHaveYearAndAllHaveWriter;
                }
                if (notAllHaveWriter)
                    return GroupType.AllHaveYearAndOneHaveWriter;
                else
                    return GroupType.AllHaveYearAndAllHaveWriter;
            }
            return GroupType.None;
        }

        private static bool Check(ListViewItemSave listViewItemSave, GroupOfDupl group)
        {
            foreach (string prefix in _keepPrefix)
            {
                if (listViewItemSave.FileName.StartsWith(prefix) && !group.Items.All(i => i.FileName.StartsWith(prefix)))
                    return false;
            }

            listViewItemSave.Checked = true;
            return true;
        }

        private static bool HaveWriter(IEnumerable<ListViewItemSave> items)
        {
            return items.Any(i => StartFromWriter(Path.GetFileNameWithoutExtension(i.FileName)));
        } 

        public static bool StartFromWriter(string name)
        {
            string pattern = @"^[А-ЯЁ][а-яё]{1,19}[_ ][А-ЯЁ][_. ][А-ЯЁ]?[._]?";
            return Regex.IsMatch(name, pattern, RegexOptions.Compiled);
        }

        private static bool HaveYear(List<ListViewItemSave> items)
        {
            return items.Any(i => EndWithYear(Path.GetFileNameWithoutExtension(i.FileName), out _));
        }

        private static bool EndWithYear(string fileName, out int? yearO)
        {
            string end = fileName.Substring(fileName.Length - 4);
            if (int.TryParse(end, out int year))
            {
                if (year > 1800)
                {
                    yearO = year;
                    return true;
                }
            }

            yearO = null;
            return false;
        }
    }
}
