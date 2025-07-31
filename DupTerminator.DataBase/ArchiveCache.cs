using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.DataBase
{
    internal class ArchiveCache
    {
        private const string SQL_CREATE = @" PRAGMA synchronous = OFF;
                            PRAGMA journal_mode = OFF;
                            CREATE TABLE IF NOT EXISTS 
                            ExtendedFileInfo (Path           TEXT NOT NULL,
                                             LastWriteTime	TEXT NOT NULL,
	                                         Size	        INTEGER NOT NULL, 
                                             Md5            TEXT,
                            PRIMARY KEY(Path,LastWriteTime,Size))";
    }
}
