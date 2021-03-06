﻿//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using DataWF.Common;
using Doc.Odf;
using System;
using System.Collections.Generic;
using System.IO;

//using DataControl;

namespace DataWF.Data
{
    public class OdtFormatter : DocumentFormatter
    {
        public override string Fill(Stream stream, string fileName, ExecuteArgs param)
        {
            TextDocument doc = new TextDocument(stream);
            OdtProcessor processor = new OdtProcessor(doc);

            var procedures = new List<string>();
            var fields = processor.GetFields();
            var elements = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (string documentField in fields)
            {
                // adding group
                if (!procedures.Contains(documentField))
                {
                    object rez = ParseString(param, documentField);
                    if (rez == null)
                        continue;
                    procedures.Add(documentField);
                    elements.Add(documentField, rez);
                }
                else
                {
                    continue;
                }
            }
            processor.PerformReplace(elements);
            var tempFile = GetTempFileName(fileName);
            doc.Save(tempFile);
            return tempFile;
        }
    }


    public class OdtProcessor
    {
        public OdtProcessor(TextDocument textDoc)
        {
            this.textDoc = textDoc;
        }

        protected TextDocument textDoc;
        public TextDocument TextDoc
        {
            get { return textDoc; }
        }

        public List<string> GetFields()
        {
            // init
            List<string> fields = new List<string>();

            List<Placeholder> placeholderElements = TextDoc.GetPlaceholders();

            // creating array
            foreach (Placeholder node in placeholderElements)
            {
                // attributes

                fields.Add(node.Value);
                // getting parameters
            }
            return fields;
        }

        //public System.Drawing.Image BuildImage(byte[] img)
        //{
        //    try
        //    {
        //        using (MemoryStream ms = new MemoryStream(img))
        //            return System.Drawing.Image.FromStream(ms);
        //    }
        //    catch { return null; }
        //    //return null;
        //}

        public void PerformReplace(Dictionary<string, object> elements)
        {
            // creating stream

            //XmlNode convertationNode = xmlDocument.CreateElement("ConvertationNode");
            List<Placeholder> placeholderElements = TextDoc.GetPlaceholders();
            foreach (var pair in elements)
            {
                foreach (Placeholder node in placeholderElements)
                {
                    if (pair.Key.ToLower() != node.Value.ToLower()) continue;
                    if (pair.Value == null)
                    {
                        node.Owner.Replace(node, new TextElement(TextDoc, "íåò äàííûõ"));
                    }
                    else if (node.Type == PlaceholdeType.Image)
                    {
                        if (pair.Value == null || pair.Value == DBNull.Value) continue;

                        //var img = BuildImage((byte[])pair.Value);

                        //if (img == null)
                        //    continue;
                        Frame frame = textDoc.GetFrame((byte[])pair.Value,
                                                       100, //(img.Width / ((System.Drawing.Bitmap)img).PixelWidth) * 2.54D,
                                                       100, 7);// (img.Height / ((System.Drawing.Bitmap)img).PixelHeight) * 2.54D, 7);
                        Service.Replace(node, frame);
                        //img.Dispose();
                    }
                    else if (node.Type == PlaceholdeType.Text)
                    {
                        if (pair.Value == null || pair.Value == DBNull.Value)
                            continue;
                        if (pair.Value is string)
                        {
                            textDoc.InsertText(pair.Value.ToString(), node, true);
                        }
                        else
                        {
                            //Paragraph parentParagraph = (Paragraph)Service.GetParent(node, typeof(Paragraph));
                            var values = new List<object>();
                            foreach (var dr in pair.Value as List<Dictionary<string, object>>)
                            {
                                foreach (var dc in dr)
                                {
                                    if (dc.Value.GetType() == typeof(byte[]))
                                    {
                                        //var img = BuildImage((byte[])dc.Value);
                                        //if (img != null)
                                        //{
                                        Frame frame = textDoc.GetFrame((byte[])dc.Value, 100, 100, 7);
                                        //img.Dispose();
                                        values.Add(frame);
                                        //}
                                    }
                                    else
                                    {
                                        string str = dc.Value.ToString();
                                        if (str.Length != 0)
                                        {
                                            if (values.Count == 0 || values[values.Count - 1] is BaseItem)
                                                values.Add(dc.ToString() + " ");
                                            else
                                                values[values.Count - 1] = values[values.Count - 1].ToString() + str + " ";
                                        }
                                    }
                                }
                                values.Add("\n");

                            }
                            textDoc.InsertRange(values, node, true);
                            //node.Owner.Remove(node);
                        }
                    }
                    else if (node.Type == PlaceholdeType.Table)
                    {
                        var query = pair.Value as QResult;
                        Table parentTable = Service.GetParent(node, typeof(Table)) as Table;
                        Row parentRow = Service.GetParent(node, typeof(Row)) as Row;
                        if (parentTable == null)
                        {
                            TableStyle tableStyle = new TableStyle(TextDoc);
                            tableStyle.TableProperty.Align = "center";
                            parentTable = new Table(TextDoc)
                            {
                                Style = tableStyle
                            };

                            Column column = new Column(TextDoc);
                            ColumnStyle columnStyle = new ColumnStyle(TextDoc);
                            column.Style = columnStyle;
                            column.RepeatedCount = (uint)query.Values.Count;

                            parentRow = new Row(TextDoc);

                            CellStyle cellStyle = new CellStyle(TextDoc);
                            cellStyle.ColumnProperty.BorderLeft = "0.004cm solid #000000";

                            Cell cell = new Cell(TextDoc)
                            {
                                Style = cellStyle
                            };

                            for (int i = 0; i < query.Values.Count; i++)
                                parentRow.Add((Cell)cell.Clone());
                            parentTable.Add(column);
                            parentTable.Add(parentRow);
                            Paragraph pparagraph = Service.GetParent(node, typeof(Paragraph)) as Paragraph;
                            pparagraph.Owner.Replace(pparagraph, parentTable);
                        }

                        Row prevRow = parentRow;
                        foreach (var dr in query.Values)
                        {
                            Row newRow = (Row)parentRow.Clone();
                            List<Cell> list = newRow.GetCells();
                            int i = -1;
                            foreach (var item in dr)
                            {
                                i++;
                                if (i < list.Count)
                                {
                                    if (item == null) continue;
                                    list[i].FirstParagraph.Clear();
                                    if (item is byte[])
                                    {
                                        try
                                        {
                                            //var img = BuildImage((byte[])item);
                                            //if (img != null)
                                            //{
                                            Frame frame = textDoc.GetFrame((byte[])item,
                                                           100,//(img.Width / ((System.Drawing.Bitmap)img).PixelWidth) * 2.54D,
                                                           100, 7);//(img.Height / ((System.Drawing.Bitmap)img).PixelHeight) * 2.54D, 7);
                                            list[i].FirstParagraph.Add(frame);
                                            //img.Dispose();
                                            //}
                                        }
                                        catch (Exception ex) { list[i].FirstParagraph.Add(ex.Message); }

                                    }
                                    //else if (item is System.Drawing.Image)
                                    //{
                                    //    var img = (System.Drawing.Image)item;
                                    //    Frame frame = textDoc.GetFrame(null,//TODO
                                    //                                   (img.Width / ((System.Drawing.Bitmap)img).PixelWidth) * 2.54D,
                                    //                                   (img.Height / ((System.Drawing.Bitmap)img).PixelHeight) * 2.54D, 7);
                                    //    list[i].FirstParagraph.Add(frame);
                                    //}
                                    else
                                    {
                                        list[i].FirstParagraph.Add(item.ToString());
                                    }
                                }
                            }
                            parentTable.InsertAfter(prevRow, newRow);
                            prevRow = newRow;
                        }
                        parentTable.Remove(parentRow);

                    }
                }
            }
        }
    }
}