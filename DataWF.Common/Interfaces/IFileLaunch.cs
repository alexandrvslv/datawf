﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IFileLaunch
    {
        Task<bool> Launch(string stringUri);

        Task<(Stream Stream, string FileName)> Save(string fileName);

        Task<(Stream Stream, string FileName)> Open();

        Task<List<(Stream Stream, string FileName)>> OpenSeveral();
    }
}
