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
using DataWF.Data;
using DataWF.Common;
using System.Linq;

namespace DataWF.Module.Flow
{
    public class CheckSystemStage : IExecutable
    {
        public object Execute(ExecuteArgs parameters)
        {
            var filter = new QQuery(string.Empty, DocumentWork.DBTable);
            filter.BuildPropertyParam(nameof(DocumentWork.IsComplete), CompareType.Equal, false);
            filter.BuildPropertyParam(nameof(DocumentWork.IsSystem), CompareType.Equal, true);
            //string filter = string.Format("{0}!='{1}' and {2} in (select {3} from {4} where {5} = '{6}')",
            var wors = DocumentWork.DBTable.Load(filter, DBLoadParam.Load | DBLoadParam.Synchronize);
            foreach (DocumentWork work in wors)
            {
                if (work.Stage == null || work.Document == null)
                    continue;
                using (var transaction = new DBTransaction(Document.DBTable.Schema.Connection))
                {
                    var param = new DocumentExecuteArgs { Document = work.Document, Work = work, Stage = work.Stage };
                    Document.ExecuteStageProcedure(param, work.Stage.GetProceduresByType(ParamProcudureType.Manual));
                    work.Document.Save();

                    transaction.Commit();
                }
            }
            return null;
        }

    }
}
