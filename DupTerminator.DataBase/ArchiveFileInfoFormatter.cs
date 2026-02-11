//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using DupTerminator.BusinessLogic.Model;
//using MessagePack;
//using MessagePack.Formatters;

//namespace DupTerminator.DataBase
//{
//    public class ArchiveFileInfoFormatter : IMessagePackFormatter<ArchiveFileInfo>
//    {
//        public void Serialize(ref MessagePackWriter writer, ArchiveFileInfo value,
//            MessagePackSerializerOptions options)
//        {
//            if (value == null)
//            {
//                writer.WriteNil();
//                return;
//            }

//            writer.WriteArrayHeader(14);  // Updated array size
//            writer.Write(value.Path);
//            writer.Write(value.Name);
//            writer.Write(value.Size);
//            writer.Write(value.LastWriteTime.Ticks);
//            writer.Write(value.DirectoryName);
//            writer.Write(value.Extension);
//            writer.Write(value.ContainerFilesCount);

//            // Serialize Container
//            if (value.Container == null)
//            {
//                writer.WriteNil();
//            }
//            else
//            {
//                writer.WriteArrayHeader(6);
//                writer.Write(value.Container.Path);
//                writer.Write(value.Container.Name);
//                writer.Write(value.Container.Size);
//                writer.Write(value.Container.LastWriteTime.Ticks);
//                writer.Write(value.Container.DirectoryName);
//                writer.Write(value.Container.Extension);
//            }

//            // Archive-specific properties
//            writer.Write(value.ArchiveCRC);
//            writer.Write(value.ArchivePath);
//            writer.Write(value.ArchiveExtension);
//            writer.Write(value.ArchiveFileName);
//            writer.Write(value.ArchiveInArchive);

//            // Container files array
//            if (value.ContainerFiles == null)
//            {
//                writer.WriteNil();
//            }
//            else
//            {
//                writer.WriteArrayHeader(value.ContainerFiles.Length);
//                foreach (var file in value.ContainerFiles)
//                {
//                    writer.WriteArrayHeader(3);
//                    writer.Write(file.Path);
//                    writer.Write(file.Name);
//                    writer.Write(file.ArchivePath);
//                }
//            }
//        }

//        public ArchiveFileInfo Deserialize(ref MessagePackReader reader,
//            MessagePackSerializerOptions options)
//        {
//            if (reader.TryReadNil())
//                return null;

//            var count = reader.ReadArrayHeader();
//            if (count != 14)
//                throw new MessagePackSerializationException("Invalid array count for ArchiveFileInfo");

//            var info = new ArchiveFileInfo
//            {
//                Path = reader.ReadString(),
//                Name = reader.ReadString(),
//                Size = reader.ReadUInt64(),
//                LastWriteTime = new DateTime(reader.ReadInt64()),
//                DirectoryName = reader.ReadString(),
//                Extension = reader.ReadString(),
//                ContainerFilesCount = reader.ReadInt32()
//            };

//            // Deserialize Container
//            if (!reader.TryReadNil())
//            {
//                var containerCount = reader.ReadArrayHeader();
//                if (containerCount != 6)
//                    throw new MessagePackSerializationException("Invalid array count for ExtendedFileInfo container");

//                info.Container = new ExtendedFileInfo
//                {
//                    Path = reader.ReadString(),
//                    Name = reader.ReadString(),
//                    Size = reader.ReadUInt64(),
//                    LastWriteTime = new DateTime(reader.ReadInt64()),
//                    DirectoryName = reader.ReadString(),
//                    Extension = reader.ReadString()
//                };
//            }

//            // Read archive-specific properties
//            info.ArchiveCRC = reader.ReadUInt32();
//            info.ArchivePath = reader.ReadString();
//            info.ArchiveExtension = reader.ReadString();
//            info.ArchiveFileName = reader.ReadString();
//            info.ArchiveInArchive = reader.ReadBoolean();

//            // Read container files array
//            if (!reader.TryReadNil())
//            {
//                var filesCount = reader.ReadArrayHeader();
//                info.ContainerFiles = new ArchiveSimpleFileInfo[filesCount];

//                for (int i = 0; i < filesCount; i++)
//                {
//                    var fileHeader = reader.ReadArrayHeader();
//                    if (fileHeader != 3)
//                        throw new MessagePackSerializationException("Invalid array count for ArchiveSimpleFileInfo");

//                    info.ContainerFiles[i] = new ArchiveSimpleFileInfo
//                    {
//                        Path = reader.ReadString(),
//                        Name = reader.ReadString(),
//                        ArchivePath = reader.ReadString()
//                    };
//                }
//            }

//            return info;
//        }
//    }
//}
