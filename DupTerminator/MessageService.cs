using DupTerminator.BusinessLogic;
using DupTerminator.Localize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator
{
    internal class MessageService : IMessageService
    {
        public void DeletedOutdatedRecord(uint deleted)
        {
            MessageBox.Show(String.Format(LanguageManager.GetString("OutdateRecordDel")));
        }
    }
}
