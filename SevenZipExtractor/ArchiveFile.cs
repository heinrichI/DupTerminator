using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace SevenZipExtractor
{
    public class ArchiveFile : IDisposable
    {
        private SevenZipHandle _sevenZipHandle;
        private readonly IInArchive _archive;
        private readonly InStreamWrapper _archiveStream;
        private IList<Entry> _entries;

        private string _libraryFilePath;

        public ArchiveFile(string archiveFilePath, string libraryFilePath = null)
        {
            this._libraryFilePath = libraryFilePath;

            this.InitializeAndValidateLibrary();

            if (!File.Exists(archiveFilePath))
            {
                throw new SevenZipException("Archive file not found");
            }

            SevenZipFormat format;
            string extension = Path.GetExtension(archiveFilePath);

            if (GuessFormatFromExtension(extension, out format))
            {
                // great
            }
            else if (GuessFormatFromSignature(archiveFilePath, out format))
            {
                // success
            }
            else
            {
                throw new SevenZipException(Path.GetFileName(archiveFilePath) + " is not a known archive type");
            }

            this._archive = this._sevenZipHandle.CreateInArchive(Formats.FormatGuidMapping[format]);
            this._archiveStream = new InStreamWrapper(File.OpenRead(archiveFilePath));
        }

        public ArchiveFile(Stream archiveStream, SevenZipFormat? format = null, string libraryFilePath = null)
        {
            this._libraryFilePath = libraryFilePath;

            this.InitializeAndValidateLibrary();

            if (archiveStream == null)
            {
                throw new SevenZipException("archiveStream is null");
            }

            if (format == null)
            {
                SevenZipFormat guessedFormat;

                if (GuessFormatFromSignature(archiveStream, out guessedFormat))
                {
                    format = guessedFormat;
                }
                else
                {
                    throw new SevenZipException("Unable to guess format automatically");
                }
            }

            this._archive = this._sevenZipHandle.CreateInArchive(Formats.FormatGuidMapping[format.Value]);
            this._archiveStream = new InStreamWrapper(archiveStream);
        }

        public void Extract(string outputFolder, bool overwrite = false)
        {
            this.Extract(entry =>
            {
                string fileName = Path.Combine(outputFolder, entry.FileName);

                if (entry.IsFolder)
                {
                    return fileName;
                }

                if (!File.Exists(fileName) || overwrite)
                {
                    return fileName;
                }

                return null;
            });
        }

        public void Extract(Func<Entry, string> getOutputPath)
        {
            IList<Stream> fileStreams = new List<Stream>();

            try
            {
                foreach (Entry entry in Entries)
                {
                    string outputPath = getOutputPath(entry);

                    if (outputPath == null) // getOutputPath = null means SKIP
                    {
                        fileStreams.Add(null);
                        continue;
                    }

                    if (entry.IsFolder)
                    {
                        Directory.CreateDirectory(outputPath);
                        fileStreams.Add(null);
                        continue;
                    }

                    string directoryName = Path.GetDirectoryName(outputPath);

                    if (!string.IsNullOrWhiteSpace(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    fileStreams.Add(File.Create(outputPath));
                }

                this._archive.Extract(null, 0xFFFFFFFF, 0, new ArchiveStreamsCallback(fileStreams));
            }
            finally
            {
                foreach (Stream stream in fileStreams)
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                }
            }
        }

        public IList<Entry> Entries
        {
            get
            {
                if (this._entries != null)
                {
                    return this._entries;
                }

                ulong checkPos = 32 * 1024;
                int open = this._archive.Open(this._archiveStream, ref checkPos, null);

                if (open != 0)
                {
                    throw new SevenZipException("Unable to open archive");
                }

                uint itemsCount = this._archive.GetNumberOfItems();

                this._entries = new List<Entry>();

                for (uint fileIndex = 0; fileIndex < itemsCount; fileIndex++)
                {
                    string fileName = this.GetProperty<string>(fileIndex, ItemPropId.kpidPath);
                    bool isFolder = this.GetProperty<bool>(fileIndex, ItemPropId.kpidIsFolder);
                    bool isEncrypted = this.GetProperty<bool>(fileIndex, ItemPropId.kpidEncrypted);
                    ulong size = this.GetProperty<ulong>(fileIndex, ItemPropId.kpidSize);
                    ulong packedSize = this.GetProperty<ulong>(fileIndex, ItemPropId.kpidPackedSize);
                    DateTime creationTime = this.GetPropertySafe<DateTime>(fileIndex, ItemPropId.kpidCreationTime);
                    DateTime lastWriteTime = this.GetPropertySafe<DateTime>(fileIndex, ItemPropId.kpidLastWriteTime);
                    DateTime lastAccessTime = this.GetPropertySafe<DateTime>(fileIndex, ItemPropId.kpidLastAccessTime);
                    uint crc = this.GetPropertySafe<uint>(fileIndex, ItemPropId.kpidCRC);
                    uint attributes = this.GetPropertySafe<uint>(fileIndex, ItemPropId.kpidAttributes);
                    string comment = this.GetPropertySafe<string>(fileIndex, ItemPropId.kpidComment);
                    string hostOS = this.GetPropertySafe<string>(fileIndex, ItemPropId.kpidHostOS);
                    string method = this.GetPropertySafe<string>(fileIndex, ItemPropId.kpidMethod);

                    bool isSplitBefore = this.GetPropertySafe<bool>(fileIndex, ItemPropId.kpidSplitBefore);
                    bool isSplitAfter = this.GetPropertySafe<bool>(fileIndex, ItemPropId.kpidSplitAfter);

                    this._entries.Add(new Entry(this._archive, fileIndex)
                    {
                        FileName = fileName,
                        IsFolder = isFolder,
                        IsEncrypted = isEncrypted,
                        Size = size,
                        PackedSize = packedSize,
                        CreationTime = creationTime,
                        LastWriteTime = lastWriteTime,
                        LastAccessTime = lastAccessTime,
                        CRC = crc,
                        Attributes = attributes,
                        Comment = comment,
                        HostOS = hostOS,
                        Method = method,
                        IsSplitBefore = isSplitBefore,
                        IsSplitAfter = isSplitAfter
                    });
                }

                return this._entries;
            }
        }

        private T GetPropertySafe<T>(uint fileIndex, ItemPropId name)
        {
            try
            {
                return this.GetProperty<T>(fileIndex, name);
            }
            catch (InvalidCastException)
            {
                return default(T);
            }
        }

        private T GetProperty<T>(uint fileIndex, ItemPropId name)
        {
            PropVariant propVariant = new PropVariant();
            this._archive.GetProperty(fileIndex, name, ref propVariant);
            object value = propVariant.GetObject();

            if (propVariant.VarType == VarEnum.VT_EMPTY)
            {
                propVariant.Clear();
                return default(T);
            }

            propVariant.Clear();

            if (value == null)
            {
                return default(T);
            }

            Type type = typeof(T);
            bool isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            Type underlyingType = isNullable ? Nullable.GetUnderlyingType(type) : type;

            T result = (T)Convert.ChangeType(value.ToString(), underlyingType);

            return result;
        }

        private void InitializeAndValidateLibrary()
        {
            if (string.IsNullOrWhiteSpace(this._libraryFilePath))
            {
                string currentArchitecture = IntPtr.Size == 4 ? "x86" : "x64"; // magic check

                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z-" + currentArchitecture + ".dll")))
                {
                    this._libraryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z-" + currentArchitecture + ".dll");
                }
                else if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "7z-" + currentArchitecture + ".dll")))
                {
                    this._libraryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "7z-" + currentArchitecture + ".dll");
                }
                else if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", currentArchitecture, "7z.dll")))
                {
                    this._libraryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", currentArchitecture, "7z.dll");
                }
                else if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, currentArchitecture, "7z.dll")))
                {
                    this._libraryFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, currentArchitecture, "7z.dll");
                }
                else if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.dll")))
                {
                    this._libraryFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.dll");
                }
            }

            if (string.IsNullOrWhiteSpace(this._libraryFilePath))
            {
                throw new SevenZipException("libraryFilePath not set");
            }

            if (!File.Exists(this._libraryFilePath))
            {
                throw new SevenZipException("7z.dll not found");
            }

            try
            {
                this._sevenZipHandle = new SevenZipHandle(this._libraryFilePath);
            }
            catch (Exception e)
            {
                throw new SevenZipException("Unable to initialize SevenZipHandle", e);
            }
        }

        private static bool GuessFormatFromExtension(string fileExtension, out SevenZipFormat format)
        {
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                format = SevenZipFormat.Undefined;
                return false;
            }

            fileExtension = fileExtension.TrimStart('.').Trim().ToLowerInvariant();

            if (fileExtension.Equals("rar"))
            {
                // 7z has different GUID for Pre-RAR5 and RAR5, but they have both same extension (.rar)
                // If it is [0x52 0x61 0x72 0x21 0x1A 0x07 0x01 0x00] then file is RAR5 otherwise RAR.
                // https://www.rarlab.com/technote.htm

                // We are unable to guess right format just by looking at extension and have to check signature

                format = SevenZipFormat.Undefined;
                return false;
            }

            if (!Formats.ExtensionFormatMapping.ContainsKey(fileExtension))
            {
                format = SevenZipFormat.Undefined;
                return false;
            }

            format = Formats.ExtensionFormatMapping[fileExtension];
            return true;
        }


        private static bool GuessFormatFromSignature(string filePath, out SevenZipFormat format)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return GuessFormatFromSignature(fileStream, out format);
            }
        }

        private static bool GuessFormatFromSignature(Stream stream, out SevenZipFormat format)
        {
            int longestSignature = Formats.FileSignatures.Values.OrderByDescending(v => v.Length).First().Length;

            byte[] archiveFileSignature = new byte[longestSignature];
            int bytesRead = stream.Read(archiveFileSignature, 0, longestSignature);

            stream.Position -= bytesRead; // go back o beginning

            if (bytesRead != longestSignature)
            {
                format = SevenZipFormat.Undefined;
                return false;
            }

            foreach (KeyValuePair<SevenZipFormat, byte[]> pair in Formats.FileSignatures)
            {
                if (archiveFileSignature.Take(pair.Value.Length).SequenceEqual(pair.Value))
                {
                    format = pair.Key;
                    return true;
                }
            }

            format = SevenZipFormat.Undefined;
            return false;
        }

        ~ArchiveFile()
        {
            this.Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if (this._archiveStream != null)
            {
                this._archiveStream.Dispose();
            }

            if (this._archive != null)
            {
                Marshal.ReleaseComObject(this._archive);
            }

            if (this._sevenZipHandle != null)
            {
                this._sevenZipHandle.Dispose();
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public static bool IsArchive(string filePath)
        {
            string extension = Path.GetExtension(filePath);

            if (GuessFormatFromExtension(extension, out _))
            {
                // great
                return true;
            }
            else if (GuessFormatFromSignature(filePath, out _))
            {
                // success
                return true;
            }

            return false;
        }
    }
}
