using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.DataBase.Dto
{
    public static class ArchiveInfoMapper
    {
        // ArchiveFileInfo <-> ArchiveFileInfoDto
        public static ArchiveFileInfoDto ToDto(this ArchiveFileInfo source)
        {
            if (source == null) return null!;

            return new ArchiveFileInfoDto
            {
                Size = source.Size,
                Name = source.Name,
                Path = source.Path,
                LastWriteTime = source.LastWriteTime,
                DirectoryName = source.DirectoryName,
                Extension = source.Extension,
                ContainerFilesCount = source.ContainerFilesCount,
                Container = source.Container?.ToExtendedDto(),
                ArchiveCRC = source.ArchiveCRC,
                ArchivePath = source.ArchivePath,
                ArchiveExtension = source.ArchiveExtension,
                ArchiveFileName = source.ArchiveFileName,
                ArchiveInArchive = source.ArchiveInArchive,
                ContainerFiles = source.ContainerFiles?.Select(x => x.ToDto()).ToArray()
            };
        }

        public static ArchiveFileInfo ToEntity(this ArchiveFileInfoDto source)
        {
            if (source == null) return null!;

            return new ArchiveFileInfo
            {
                Size = source.Size,
                Name = source.Name,
                Path = source.Path,
                LastWriteTime = source.LastWriteTime,
                DirectoryName = source.DirectoryName,
                Extension = source.Extension,
                ContainerFilesCount = source.ContainerFilesCount,
                Container = source.Container?.ToEntity(),
                ArchiveCRC = source.ArchiveCRC,
                ArchivePath = source.ArchivePath,
                ArchiveExtension = source.ArchiveExtension,
                ArchiveFileName = source.ArchiveFileName,
                ArchiveInArchive = source.ArchiveInArchive,
                ContainerFiles = source.ContainerFiles?.Select(x => x.ToEntity()).ToArray()
            };
        }

        // ExtendedFileInfo <-> ExtendedFileInfoDto
        public static ExtendedFileInfoDto ToExtendedDto(this ExtendedFileInfo source)
        {
            if (source == null) return null!;

            return new ExtendedFileInfoDto
            {
                Size = source.Size,
                Name = source.Name,
                Path = source.Path,
                LastWriteTime = source.LastWriteTime,
                DirectoryName = source.DirectoryName,
                Extension = source.Extension,
                ContainerFilesCount = source.ContainerFilesCount,
                Container = source.Container?.ToExtendedDto()
            };
        }

        public static ExtendedFileInfo ToEntity(this ExtendedFileInfoDto source)
        {
            if (source == null) return null!;

            return new ExtendedFileInfo
            {
                Size = source.Size,
                Name = source.Name,
                Path = source.Path,
                LastWriteTime = source.LastWriteTime,
                DirectoryName = source.DirectoryName,
                Extension = source.Extension,
                ContainerFilesCount = source.ContainerFilesCount,
                Container = source.Container?.ToEntity()
            };
        }

        // ArchiveSimpleFileInfo <-> ArchiveSimpleFileInfoDto
        public static ArchiveSimpleFileInfoDto ToDto(this ArchiveSimpleFileInfo source)
        {
            if (source == null) return null!;

            return new ArchiveSimpleFileInfoDto
            {
                Size = source.Size,
                Name = source.Name,
                Path = source.Path,
                ArchivePath = source.ArchivePath
            };
        }

        public static ArchiveSimpleFileInfo ToEntity(this ArchiveSimpleFileInfoDto source)
        {
            if (source == null) return null!;

            return new ArchiveSimpleFileInfo
            {
                Size = source.Size,
                Name = source.Name,
                Path = source.Path,
                ArchivePath = source.ArchivePath
            };
        }

        // Array conversions
        public static ArchiveFileInfoDto[] ToDtoArray(this IEnumerable<ArchiveFileInfo> source)
        {
            if (source == null) return null!;
            return source.Select(x => x.ToDto()).ToArray();
        }

        public static ArchiveFileInfo[] ToEntityArray(this ArchiveFileInfoDto[] source)
        {
            if (source == null) return null!;
            return source.Select(x => x.ToEntity()).ToArray();
        }
    }
}
