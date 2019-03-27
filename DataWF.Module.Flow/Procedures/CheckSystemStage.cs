/*
 FlowEnvir.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>  

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using DataWF.Common;
using DataWF.Data;
using System.Threading.Tasks;

namespace DataWF.Module.Flow
{
    public class CheckSystemStage : IExecutable
    {
        public async Task<object> Execute(ExecuteArgs parameters)
        {
            var filter = new QQuery(string.Empty, DocumentWork.DBTable);
            filter.BuildPropertyParam(nameof(DocumentWork.IsComplete), CompareType.Equal, false);
            filter.BuildPropertyParam(nameof(DocumentWork.IsSystem), CompareType.Equal, true);
            //string filter = string.Format("{0}!='{1}' and {2} in (select {3} from {4} where {5} = '{6}')",
            foreach (DocumentWork work in DocumentWork.DBTable.Load(filter, DBLoadParam.Load | DBLoadParam.Synchronize, parameters.Transaction))
            {
                if (work.Stage == null || work.Document == null)
                    continue;
                work.Document.ExecuteProceduresByWork(work, StageParamProcudureType.Manual, parameters.Transaction);
                await work.Document.Save(parameters.Transaction);
            }
            return null;
        }

    }
}
