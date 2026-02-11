//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using DupTerminator.BusinessLogic.Model;
//using MessagePack;
//using MessagePack.Formatters;
//using MessagePack.Resolvers;

//namespace DupTerminator.DataBase
//{
//    // Register the formatter
//    public class CustomResolver : IFormatterResolver
//    {
//        public static readonly CustomResolver Instance = new();

//        private CustomResolver() { }

//        public IMessagePackFormatter<T> GetFormatter<T>()
//        {
//            if (typeof(T) == typeof(ArchiveFileInfo))
//                return (IMessagePackFormatter<T>)(object)new ArchiveFileInfoFormatter();

//            return ContractlessStandardResolver.Instance.GetFormatter<T>();
//        }
//    }
//}
