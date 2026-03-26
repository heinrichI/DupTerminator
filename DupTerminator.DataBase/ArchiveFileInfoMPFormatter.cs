using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using DupTerminator.BusinessLogic.Model;
using MemoryPack;

namespace DupTerminator.DataBase
{
    public class ArchiveFileInfoMPFormatter2 : MemoryPackFormatter<ArchiveFileInfo>
    {
        const ushort ARCHIVE_CONTAINER_TAG = 0;
        const ushort DIRECTORY_CONTAINER_TAG = 1;

        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ArchiveFileInfo? value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            // Define member count (Total logical groups/fields being written)
            // 1.Path 2.Name 3.Size 4.LastWriteTime 5.DirectoryName 6.Extension 7.ContainerFilesCount
            // 8.Container 9.ArchiveCRC 10.ArchivePath 11.ArchiveExtension 12.ArchiveFileName 13.ArchiveInArchive 
            writer.WriteObjectHeader(13);

            writer.WriteString(value.Path);
            writer.WriteString(value.Name);
            writer.WriteVarInt(value.Size);
            writer.WriteVarInt(value.LastWriteTime.Ticks);
            writer.WriteString(value.DirectoryName);
            writer.WriteString(value.Extension);
            //writer.WriteVarInt(value.ContainerFilesCount);  //7

            //Serialize Container
            if (value.Container == null)
            {
                writer.WriteNullObjectHeader();
            }
            else
            {
                // Wrap manually serialized object in a header (6 fields)
                writer.WriteObjectHeader(6);

                if (value.Container is ArchiveContainer)
                {
                    writer.WriteUnionHeader(ARCHIVE_CONTAINER_TAG); // the union header is the key to polymorphism!
                    //writer.WriteVarInt(ARCHIVE_CONTAINER_TAG);
                }
                else if (value.Container is DirectoryContainer)
                {
                    writer.WriteUnionHeader(DIRECTORY_CONTAINER_TAG);
                    //writer.WriteVarInt(DIRECTORY_CONTAINER_TAG);
                }
                else
                {
                    throw new NotSupportedException("Контейнер неизвестен");
                }

                Debug.Assert(value.Container.Path != null);
                writer.WriteString(value.Container.Path);
                writer.WriteString(value.Container.Name);
                writer.WriteVarInt(value.Container.Size);
                writer.WriteVarInt(value.Container.LastWriteTime.Ticks);
                writer.WriteString(value.Container.DirectoryName);
                writer.WriteString(value.Container.Extension);
                //writer.WriteVarInt(value.Container.FilesCount);


                //Container files array
                //WriteContainerFiles(writer, value.Container.Files);
                if (value.Container.Files == null)
                {
                    writer.WriteNullCollectionHeader();
                }
                else
                {
                    //var formatter = MemoryPackFormatterProvider.GetFormatter<SimpleFileInfo>();
                    //writer.WriteArray(value.Container.Files);
                    writer.WriteCollectionHeader(value.Container.Files.Length);
                    foreach (SimpleFileInfo item in value.Container.Files)
                    {
                        //writer.WriteObjectHeader(3);
                        writer.WriteString(item.Path);
                        writer.WriteString(item.Name);
                        writer.WriteVarInt(item.Size);
                    }
                }


                if (value.Container.Container == null)
                {
                    writer.WriteNullObjectHeader();
                }
                else
                {
                    // Wrap manually serialized object in a header (6 fields)
                    writer.WriteObjectHeader(6);

                    if (value.Container.Container is ArchiveContainer)
                    {
                        //writer.WriteUnionHeader(ARCHIVE_CONTAINER_TAG); // the union header is the key to polymorphism!
                        writer.WriteVarInt(ARCHIVE_CONTAINER_TAG);
                    }
                    else if (value.Container.Container is DirectoryContainer)
                    {
                        //writer.WriteUnionHeader(DIRECTORY_CONTAINER_TAG);
                        writer.WriteVarInt(DIRECTORY_CONTAINER_TAG);
                    }
                    else
                    {
                        throw new NotSupportedException("Контейнер неизвестен");
                    }

                    writer.WriteString(value.Container.Container.Path);
                    writer.WriteString(value.Container.Container.Name);
                    writer.WriteVarInt(value.Container.Container.Size);
                    writer.WriteVarInt(value.Container.Container.LastWriteTime.Ticks);
                    writer.WriteString(value.Container.Container.DirectoryName);
                    writer.WriteString(value.Container.Container.Extension);
                    //writer.WriteVarInt(value.Container.Container.FilesCount);

                    if (value.Container.Container.Files == null)
                    {
                        writer.WriteNullCollectionHeader();
                    }
                    //else
                    //{
                    //    writer.WriteArray(value.Container.Container.Files);
                    //}
                    else
                    {
                        writer.WriteCollectionHeader(value.Container.Container.Files.Length);
                        foreach (var item in value.Container.Container.Files)
                        {
                            //writer.WriteObjectHeader(3);
                            writer.WriteString(item.Path);
                            writer.WriteString(item.Name);
                            writer.WriteVarInt(item.Size);
                        }
                    }

                }
            }

            // Archive-specific properties
            writer.WriteVarInt(value.ArchiveCRC);
            writer.WriteString(value.ArchivePath);
            writer.WriteString(value.ArchiveExtension);
            writer.WriteString(value.ArchiveFileName);
            writer.WriteValue<bool>(value.ArchiveInArchive);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ArchiveFileInfo? value)
        {
            if (!reader.TryReadObjectHeader(out var memberCount))
            {
                value = null;
                return;
            }

            Debug.Assert(memberCount == 13);
            value = new ArchiveFileInfo
            {
                Path = reader.ReadString(),
                Name = reader.ReadString(),
                Size = reader.ReadVarIntUInt64(),
                LastWriteTime = new DateTime(reader.ReadVarIntInt64()),
                DirectoryName = reader.ReadString(),
                Extension = reader.ReadString(),
                //ContainerFilesCount = reader.ReadVarIntInt32(),
            };

            // Deserialize Container
            if (!reader.TryReadObjectHeader(out var containerCount))
            {
                value.Container = null;
            }
            else
            {
                //var tag = reader.ReadVarIntUInt16();
                if (!reader.TryReadUnionHeader(out var tag))  //  the union header is the key to polymorphism!
                {
                    //value = default;
                    //return;
                    throw new NotSupportedException("Контейнер неизвестен");
                }

                Debug.Assert(containerCount == 6);
                if (tag == ARCHIVE_CONTAINER_TAG)
                {
                    value.Container = new ArchiveContainer
                    {
                        Path = reader.ReadString(),
                        Name = reader.ReadString(),
                        Size = reader.ReadVarIntUInt64(),
                        LastWriteTime = new DateTime(reader.ReadVarIntInt64()),
                        DirectoryName = reader.ReadString(),
                        Extension = reader.ReadString(),
                        //FilesCount = reader.ReadVarIntInt32()
                    };
                }
                else if (tag == DIRECTORY_CONTAINER_TAG)
                {
                    value.Container = new DirectoryContainer
                    {
                        Path = reader.ReadString(),
                        Name = reader.ReadString(),
                        Size = reader.ReadVarIntUInt64(),
                        LastWriteTime = new DateTime(reader.ReadVarIntInt64()),
                        DirectoryName = reader.ReadString(),
                        Extension = reader.ReadString(),
                        //FilesCount = reader.ReadVarIntInt32()
                    };
                }
                else
                {
                    throw new NotSupportedException("Контейнер неизвестен");
                }

                //value.Container.Files = reader.ReadArray<ArchiveSimpleFileInfo>();
                if (reader.TryReadCollectionHeader(out var length))
                {
                    var list = new List<SimpleFileInfo>(length);
                    for (var i = 0; i < length; i++)
                    {
                        list.Add(new SimpleFileInfo
                        {
                            Path = reader.ReadString(),
                            Name = reader.ReadString(),
                            Size = reader.ReadVarIntUInt64(),
                        });
                    }

                    value.Container.Files = list.ToArray();
                }


                if (!reader.TryReadObjectHeader(out var container2Count))
                {
                    value.Container.Container = null;
                }
                else
                {
                    Debug.Assert(container2Count == 6);

                    //var tag2 = reader.ReadVarIntUInt16();
                    if (!reader.TryReadUnionHeader(out var tag2))  //  the union header is the key to polymorphism!
                    {
                        throw new NotSupportedException("Контейнер неизвестен");
                    }
                    if (tag2 == ARCHIVE_CONTAINER_TAG)
                    {
                        value.Container.Container = new ArchiveContainer
                        {
                            Path = reader.ReadString(),
                            Name = reader.ReadString(),
                            Size = reader.ReadVarIntUInt64(),
                            LastWriteTime = new DateTime(reader.ReadVarIntInt64()),
                            DirectoryName = reader.ReadString(),
                            Extension = reader.ReadString(),
                            //FilesCount = reader.ReadVarIntInt32()
                        };
                    }
                    else if (tag2 == DIRECTORY_CONTAINER_TAG)
                    {
                        value.Container.Container = new DirectoryContainer
                        {
                            Path = reader.ReadString(),
                            Name = reader.ReadString(),
                            Size = reader.ReadVarIntUInt64(),
                            LastWriteTime = new DateTime(reader.ReadVarIntInt64()),
                            DirectoryName = reader.ReadString(),
                            Extension = reader.ReadString(),
                            //FilesCount = reader.ReadVarIntInt32()
                        };
                    }
                    else
                    {
                        throw new NotSupportedException("Контейнер неизвестен");
                    }


                    if (reader.TryReadCollectionHeader(out var length2))
                    {
                        var list = new List<SimpleFileInfo>(length2);
                        for (var i = 0; i < length2; i++)
                        {
                            list.Add(new SimpleFileInfo
                            {
                                Path = reader.ReadString(),
                                Name = reader.ReadString(),
                                Size = reader.ReadVarIntUInt64(),
                            });
                        }

                        value.Container.Container.Files = list.ToArray();
                    }
                }
            }

            //// Read archive-specific properties
            value.ArchiveCRC = reader.ReadVarIntUInt32();
            value.ArchivePath = reader.ReadString();
            value.ArchiveExtension = reader.ReadString();
            value.ArchiveFileName = reader.ReadString();
            value.ArchiveInArchive = reader.ReadValue<bool>();

        }
    }
    //public class ArchiveFileInfoMPFormatter : MemoryPackFormatter<ArchiveFileInfo[]>
    //{
    //    // Unity does not support scoped and TBufferWriter so change signature to `Serialize(ref MemoryPackWriter writer, ref AnimationCurve value)`
    //    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ArchiveFileInfo[]? value)
    //    {
    //        if (value == null)
    //        {
    //            writer.WriteNullObjectHeader();
    //            return;
    //        }

    //        //writer.WritePackable(new SerializableAnimationCurve(value));

    //        writer.WriteArray<ArchiveFileInfo>(value);
    //        //writer.Write(value.Name);
    //        //writer.Write(value.Size);
    //        //writer.Write(value.LastWriteTime.Ticks);
    //        //writer.Write(value.DirectoryName);
    //        //writer.Write(value.Extension);
    //        //writer.Write(value.ContainerFilesCount);
    //    }

    //    public override void Deserialize(ref MemoryPackReader reader, scoped ref ArchiveFileInfo[]? value)
    //    {
    //        if (reader.PeekIsNull())
    //        {
    //            reader.Advance(1); // skip null block
    //            value = null;
    //            return;
    //        }

    //        //var wrapped = reader.ReadPackable<SerializableAnimationCurve>();
    //        //value = wrapped.AnimationCurve;
    //        value = reader.ReadArray<ArchiveFileInfo>();
    //    }
    //}

    //public class SimpleFileInfoFormatter : MemoryPackFormatter<SimpleFileInfo>
    //{
    //Without an object header, Deserialize relies on PeekIsNull(). If the first byte of a valid property(e.g., a string with length 255)
    //the NullObject code(0xFF), PeekIsNull returns true, causing the reader to incorrectly return null and leaving unread data that corrupts subsequent reads.
    //TryReadObjectHeader handles this safely.
    //    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SimpleFileInfo? value)
    //    {
    //        if (value == null)
    //        {
    //            writer.WriteNullObjectHeader();
    //            return;
    //        }


    //        Debug.Assert(value.Name is not null);
    //        Debug.Assert(value.Path is not null);

    //        // Write object header with member count (3 fields) to enable safe null checking on read
    //        writer.WriteObjectHeader(3);
    //        writer.WriteString(value.Path);
    //        writer.WriteString(value.Name);
    //        writer.WriteVarInt(value.Size);
    //    }

    //    public override void Deserialize(ref MemoryPackReader reader, scoped ref SimpleFileInfo? value)
    //    {
    //        // Use TryReadObjectHeader to safely detects nulls or start reading members
    //        if (!reader.TryReadObjectHeader(out var memberCount))
    //        {
    //            value = null;
    //            return;
    //        }

    //        value = new SimpleFileInfo
    //        {
    //            Path = reader.ReadString(),
    //            Name = reader.ReadString(),
    //            Size = reader.ReadVarIntUInt64(),
    //        };
    //    }
    //}

    //public class ArchiveSimpleFileInfoFormatter : MemoryPackFormatter<ArchiveSimpleFileInfo>
    //{
    //    //Without an object header, Deserialize relies on PeekIsNull(). If the first byte of a valid property(e.g., a string with length 255)
    //    //the NullObject code(0xFF), PeekIsNull returns true, causing the reader to incorrectly return null and leaving unread data that corrupts subsequent reads.
    //    //TryReadObjectHeader handles this safely.
    //    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ArchiveSimpleFileInfo? value)
    //    {
    //        if (value == null)
    //        {
    //            writer.WriteNullObjectHeader();
    //            return;
    //        }


    //        Debug.Assert(value.Name is not null);
    //        Debug.Assert(value.Path is not null);
    //        Debug.Assert(value.ArchivePath is not null);

    //        // Write object header with member count (3 fields) to enable safe null checking on read
    //        writer.WriteObjectHeader(3);
    //        writer.WriteString(value.Path);
    //        writer.WriteString(value.Name);
    //        writer.WriteVarInt(value.Size);
    //    }

    //    public override void Deserialize(ref MemoryPackReader reader, scoped ref ArchiveSimpleFileInfo? value)
    //    {
    //        // Use TryReadObjectHeader to safely detects nulls or start reading members
    //        if (!reader.TryReadObjectHeader(out var memberCount))
    //        {
    //            value = null;
    //            return;
    //        }

    //        value = new ArchiveSimpleFileInfo
    //        {
    //            Path = reader.ReadString(),
    //            Name = reader.ReadString(),
    //            Size = reader.ReadVarIntUInt64(),
    //        };
    //    }
    //}
    //public class ExtendedFileInfoFormatter : MemoryPackFormatter<ExtendedFileInfo>
    //{
    //    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ExtendedFileInfo? value)
    //    {
    //        if (value == null)
    //        {
    //            writer.WriteNullObjectHeader();
    //            return;
    //        }

    //        Debug.Assert(value.Name is not null);
    //        Debug.Assert(value.Path is not null);

    //        writer.WriteString(value.Path);
    //        writer.WriteString(value.Name);
    //        writer.WriteVarInt(value.Size);
    //        writer.WriteVarInt(value.LastWriteTime.Ticks);
    //        writer.WriteString(value.DirectoryName);
    //        writer.WriteString(value.Extension);
    //    }

    //    public override void Deserialize(ref MemoryPackReader reader, scoped ref ExtendedFileInfo? value)
    //    {
    //        if (reader.PeekIsNull())
    //        {
    //            reader.Advance(1); // skip null block
    //            value = null;
    //            return;
    //        }

    //        value = new ExtendedFileInfo
    //        {
    //            Path = reader.ReadString(),
    //            Name = reader.ReadString(),
    //            Size = reader.ReadVarIntUInt64(),
    //            LastWriteTime = new DateTime(reader.ReadVarIntInt64()),
    //            DirectoryName = reader.ReadString(),
    //            Extension = reader.ReadString()
    //        };
    //    }
    //}
}
