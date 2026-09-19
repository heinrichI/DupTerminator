using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    public class ContainerInfo : ExtendedFileInfo, IContainerInfo
    {
        [JsonInclude]
        public SimpleFileInfo[] Files { get; set; }
        [JsonIgnore]
        public int FilesCount => Files?.Length ?? 0;
    }
}
