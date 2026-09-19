using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.BusinessLogic.Model.Modes
{
    public class PHashSearchImageResult : ResultBase
    {
        public PHashSearchImageResult(ReadOnlyCollection<PHashFileInfoSearchItem> images)
        {
            Images = images;
        }

        public ReadOnlyCollection<PHashFileInfoSearchItem> Images { get; }
    }
}
