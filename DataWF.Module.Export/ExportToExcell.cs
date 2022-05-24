using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataWF.Module.Export
{
    public class ExportToExcell<T>
    {
        public static async Task<(MemoryStream, string)> Create(List<ExcellColumn> columns, List<T> items)
        {
            var memoryStream = new MemoryStream();
            using (SpreadsheetDocument document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();
                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();

                FileVersion fv = new FileVersion();
                fv.ApplicationName = "Microsoft Office Excel";
                worksheetPart.Worksheet = new Worksheet(new SheetData());
                WorkbookStylesPart wbsp = workbookPart.AddNewPart<WorkbookStylesPart>();

                //Создаем лист в книге
                Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = nameof(T) };
                sheets.Append(sheet);

                SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                //columns = columns.Select(x=>x.).Split(',');
                //Добавим заголовки в первую строку
                {
                    Row row = new Row() { RowIndex = 1 };
                    sheetData.Append(row);
                    var header = columns.Select(x => x.Name).ToList();
                    for (var i = 0; i < header.Count(); i++)
                    {
                        var col = header[i];
                        InsertCell(row, i, header[i], CellValues.String);
                    }
                }
                var index = 1;
                for (var i = 0; i < items.Count(); i++)
                {
                    index++;
                    var item = items[i];
                    Row row = new Row();
                    row.RowIndex = UInt32Value.FromUInt32((uint)index);
                    sheetData.Append(row);
                    var body = columns.Select(x => x.Field).ToList();
                    for (var j = 0; j < body.Count(); j++)
                    {
                        var col = body[j].Split('.');
                        object value;
                        if (col.Length > 1)
                        {
                            value = GetPropertyValue(item, col[0]);
                            if (value != null)
                            {
                                value = GetPropertyValue(value, col[1]);
                            }
                            if (value != null && value.ToString().Equals("False", StringComparison.OrdinalIgnoreCase))
                            {
                                value = "No";
                            }
                            else if (value != null && value.ToString().Equals("True", StringComparison.OrdinalIgnoreCase))
                            {
                                value = "Yes";
                            }
                            InsertCell(row, j, value != null ? value.ToString() : "N/A", CellValues.String);
                        }
                        else
                        {
                            value = GetPropertyValue(item, col[0]);
                            if (value != null && value.ToString().Equals("False", StringComparison.OrdinalIgnoreCase))
                            {
                                value = "No";
                            }
                            else if (value != null && value.ToString().Equals("True", StringComparison.OrdinalIgnoreCase))
                            {
                                value = "Yes";
                            }
                            InsertCell(row, j, value != null ? value.ToString() : "N/A", CellValues.String);
                        }
                    }
                }

                workbookPart.Workbook.Save();
                document.Close();
            }
            memoryStream.Seek(0, SeekOrigin.Begin);
            return await Task.FromResult((memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
        }

        //Добавление Ячейки в строку (На вход подаем: строку, номер колонки, тип значения, стиль)
        static void InsertCell(Row row, int cell_num, string val, CellValues type)
        {
            Cell refCell = null;
            Cell newCell = new Cell() { CellReference = cell_num.ToString() + ":" + row.RowIndex.ToString() };
            row.InsertBefore(newCell, refCell);

            // Устанавливает тип значения.
            newCell.CellValue = new CellValue(val);
            newCell.DataType = new EnumValue<CellValues>(type);

        }

        //Важный метод, при вставки текстовых значений надо использовать.
        //Метод убирает из строки запрещенные спец символы.
        //Если не использовать, то при наличии в строке таких символов, вылетит ошибка.
        static string ReplaceHexadecimalSymbols(string txt)
        {
            string r = "[\x00-\x08\x0B\x0C\x0E-\x1F\x26]";
            return Regex.Replace(txt, r, "", RegexOptions.Compiled);
        }

        public static object GetPropertyValue(object item, string propertyName)
        {
            var property = item.GetType().GetProperty(propertyName.Substring(0, 1).ToUpper() + (propertyName.Length > 1 ? propertyName.Substring(1) : ""));
            if (property == null)
                return null;
            return property.GetValue(item, null);
        }

    }

}
