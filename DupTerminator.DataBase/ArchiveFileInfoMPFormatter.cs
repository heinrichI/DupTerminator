using System;
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
    public class ArchiveSimpleFileInfoFormatter : MemoryPackFormatter<ArchiveSimpleFileInfo>
    {
        //Without an object header, Deserialize relies on PeekIsNull(). If the first byte of a valid property(e.g., a string with length 255)
        //the NullObject code(0xFF), PeekIsNull returns true, causing the reader to incorrectly return null and leaving unread data that corrupts subsequent reads.
        //TryReadObjectHeader handles this safely.
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ArchiveSimpleFileInfo? value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }


            Debug.Assert(value.Name is not null);
            Debug.Assert(value.Path is not null);
            Debug.Assert(value.ArchivePath is not null);

            // Write object header with member count (3 fields) to enable safe null checking on read
            writer.WriteObjectHeader(3);
            writer.WriteString(value.Path);
            writer.WriteString(value.Name);
            writer.WriteVarInt(value.Size);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ArchiveSimpleFileInfo? value)
        {
            // Use TryReadObjectHeader to safely detects nulls or start reading members
            if (!reader.TryReadObjectHeader(out var memberCount))
            {
                value = null;
                return;
            }

            value = new ArchiveSimpleFileInfo
            {
                Path = reader.ReadString(),
                Name = reader.ReadString(),
                Size = reader.ReadVarIntUInt64(),
            };
        }
    }
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
    public class ArchiveFileInfoMPFormatter2 : MemoryPackFormatter<ArchiveFileInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ArchiveFileInfo? value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            // Define member count (Total logical groups/fields being written)
            // 1.Path 2.Name 3.Size 4.LastWriteTime 5.DirectoryName 6.Extension 7.ContainerFilesCount
            // 8.Container 9.ArchiveCRC 10.ArchivePath 11.ArchiveExtension 12.ArchiveFileName 13.ArchiveInArchive 14.ContainerFiles
            writer.WriteObjectHeader(14);

            writer.WriteString(value.Path);
            writer.WriteString(value.Name);
            writer.WriteVarInt(value.Size);
            writer.WriteVarInt(value.LastWriteTime.Ticks);
            writer.WriteString(value.DirectoryName);
            writer.WriteString(value.Extension);
            writer.WriteVarInt(value.ContainerFilesCount);  //7

            //Serialize Container
            if (value.Container == null)
            {
                writer.WriteNullObjectHeader();
            }
            else
            {
                // Wrap manually serialized object in a header (6 fields)
                writer.WriteObjectHeader(7);

                writer.WriteString(value.Container.Path);
                writer.WriteString(value.Container.Name);
                writer.WriteVarInt(value.Container.Size);
                writer.WriteVarInt(value.Container.LastWriteTime.Ticks);
                writer.WriteString(value.Container.DirectoryName);
                writer.WriteString(value.Container.Extension);

                if (value.Container.Container == null)
                {
                    writer.WriteNullObjectHeader();
                }
                else
                {
                    // Wrap manually serialized object in a header (6 fields)
                    writer.WriteObjectHeader(6);

                    writer.WriteString(value.Container.Container.Path);
                    writer.WriteString(value.Container.Container.Name);
                    writer.WriteVarInt(value.Container.Container.Size);
                    writer.WriteVarInt(value.Container.Container.LastWriteTime.Ticks);
                    writer.WriteString(value.Container.Container.DirectoryName);
                    writer.WriteString(value.Container.Container.Extension);
                }
            }

            // Archive-specific properties
            writer.WriteVarInt(value.ArchiveCRC);
            writer.WriteString(value.ArchivePath);
            writer.WriteString(value.ArchiveExtension);
            writer.WriteString(value.ArchiveFileName);
            writer.WriteValue<bool>(value.ArchiveInArchive);

            //Container files array
            if (value.ContainerFiles == null)
            {
                writer.WriteNullCollectionHeader();
            }
            else
            {
                writer.WriteArray(value.ContainerFiles);
            }
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ArchiveFileInfo? value)
        {
            if (!reader.TryReadObjectHeader(out var memberCount))
            {
                value = null;
                return;
            }

            Debug.Assert(memberCount == 14);
            value = new ArchiveFileInfo
            {
                Path = reader.ReadString(),
                Name = reader.ReadString(),
                Size = reader.ReadVarIntUInt64(),
                LastWriteTime = new DateTime(reader.ReadVarIntInt64()),
                DirectoryName = reader.ReadString(),
                Extension = reader.ReadString(),
                ContainerFilesCount = reader.ReadVarIntInt32(),
            };

            // Deserialize Container
            if (!reader.TryReadObjectHeader(out var containerCount))
            {
                value.Container = null;
            }
            else
            {
                Debug.Assert(containerCount == 7);
                value.Container = new ExtendedFileInfo
                {
                    Path = reader.ReadString(),
                    Name = reader.ReadString(),
                    Size = reader.ReadVarIntUInt64(),
                    LastWriteTime = new DateTime(reader.ReadVarIntInt64()),
                    DirectoryName = reader.ReadString(),
                    Extension = reader.ReadString()
                };

                if (!reader.TryReadObjectHeader(out var container2Count))
                {
                    value.Container.Container = null;
                }
                else
                {
                    Debug.Assert(container2Count == 6);
                    value.Container.Container = new ExtendedFileInfo
                    {
                        Path = reader.ReadString(),
                        Name = reader.ReadString(),
                        Size = reader.ReadVarIntUInt64(),
                        LastWriteTime = new DateTime(reader.ReadVarIntInt64()),
                        DirectoryName = reader.ReadString(),
                        Extension = reader.ReadString()
                    };
                }
            }

            //// Read archive-specific properties
            value.ArchiveCRC = reader.ReadVarIntUInt32();
            value.ArchivePath = reader.ReadString();
            value.ArchiveExtension = reader.ReadString();
            value.ArchiveFileName = reader.ReadString();
            value.ArchiveInArchive = reader.ReadValue<bool>();

            value.ContainerFiles = reader.ReadArray<ArchiveSimpleFileInfo>();
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
}
