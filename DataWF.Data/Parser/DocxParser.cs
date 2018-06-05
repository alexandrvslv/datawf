/*
 TemplateParcer.cs
 
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

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using Word = DocumentFormat.OpenXml.Wordprocessing;

//using DataControl;

namespace DataWF.Data
{
    public class DocxParser : DocumentParser
    {
        public override byte[] Parse(byte[] data, ExecuteArgs param)
        {
            byte[] temp = null;
            using (var ms = new MemoryStream())
            {
                ms.Write(data, 0, data.Length);
                ms.Position = 0;
                using (var wd = WordprocessingDocument.Open(ms, true))
                {
                    ParseDocxPart(wd.MainDocumentPart.Document, param);
                    foreach (var header in wd.MainDocumentPart.HeaderParts)
                        ParseDocxPart(header.Header, param);

                }
                ms.Flush();
                temp = ms.ToArray();
            }
            return temp;
        }

        public void ParseDocxPart(OpenXmlPartRootElement doc, ExecuteArgs param)
        {
            var list = new List<Word.SdtElement>();
            Find<Word.SdtElement>(doc, list);
            foreach (var item in list)
            {
                OpenXmlElement element = FindChild<Word.SdtContentBlock>(item);
                if (element == null)
                    element = FindChild<Word.SdtContentRun>(item);
                var prop = FindChild<Word.SdtProperties>(item);
                var tag = prop.GetFirstChild<Word.Tag>();
                if (tag == null)
                    tag = FindChild<Word.Tag>(element);

                if (tag != null)
                {
                    object val = ParseString(param, tag.Val.ToString());
                    if (val != null)
                    {
                        if (val is QResult)
                            FillTable(item, (QResult)val);
                        else
                            ReplaceString(element, val.ToString());
                    }
                }
            }
            doc.Save();
        }

        public void FillTable(OpenXmlElement element, QResult query)
        {
            var row = FindParent<Word.TableRow>(element);
            var prg = FindChild<Word.Paragraph>(row);
            //element.Remove();
            Word.TableRow prow = null;
            foreach (object[] data in query.Values)
            {
                Word.TableCell cell = row.GetFirstChild<Word.TableCell>();
                foreach (object value in data)
                {
                    if (cell != null)
                    {
                        var paragraph = FindChild<Word.Paragraph>(cell);
                        if (paragraph == null)
                        {
                            paragraph = (Word.Paragraph)prg.Clone();
                            cell.Append(paragraph);
                        }
                        Word.Run run = FindChild<Word.Run>(paragraph);
                        if (run != null)
                        {
                            ReplaceString(run, value.ToString());
                        }
                        else
                        {
                            Word.Text text = new Word.Text() { Text = value.ToString(), Space = SpaceProcessingModeValues.Preserve };
                            run = new Word.Run();
                            run.Append(text);
                            paragraph.Append(run);
                        }

                        cell = cell.NextSibling<Word.TableCell>();
                    }
                }
                if (row.Parent == null)
                    prow.InsertAfterSelf<Word.TableRow>(row);
                prow = row;
                row = (Word.TableRow)row.Clone();
            }
        }

        public T FindChild<T>(OpenXmlElement element) where T : OpenXmlElement
        {
            if (element is Word.DeletedRun)
            {
                element.Remove();
                return null;
            }
            if (element is T)
                return (T)element;
            var item = element.GetFirstChild<T>();

            if (item == null)
                foreach (var sub in element)
                {
                    item = FindChild<T>(sub);
                    if (item != null)
                        break;
                }
            return item;
        }

        public T FindParent<T>(OpenXmlElement element) where T : OpenXmlElement
        {
            while (!(element is T) && element.Parent != null)
                element = element.Parent;
            return element is T ? (T)element : null;
        }

        public void ReplaceString(OpenXmlElement element, string val)
        {
            var text = FindChild<Word.Text>(element);
            var run = text == null ? FindChild<Word.Run>(element) : FindParent<Word.Run>(text);
            var runp = run.Parent;
            var paragrap = FindParent<Word.Paragraph>(runp);
            run.RemoveAllChildren<Word.Text>();
            runp.RemoveAllChildren<Word.Run>();
            runp.RemoveAllChildren<Word.Break>();
            if (text == null)
                text = new Word.Text();
            else if (text.Parent != null)
                text.Remove();
            string[] pagesplit = val.TrimEnd("\r\n".ToCharArray()).Split("\f".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            for (int p = 0; p < pagesplit.Length; p++)
            {
                var temp = pagesplit[p].Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                if (temp.Length > 0)
                {
                    for (int i = 0; i < temp.Length; i++)
                    {
                        if (p == 0 && i == 0)
                        {
                            text.Text = temp[0];
                            text.Space = SpaceProcessingModeValues.Preserve;
                            run.Append(text);
                            runp.Append(run);
                            continue;
                        }
                        Word.Run r = run.Clone() as Word.Run;
                        r.RemoveAllChildren<Word.Text>();
                        r.Append(new Word.Text(temp[i]) { Space = SpaceProcessingModeValues.Preserve });

                        Word.Paragraph pr = (Word.Paragraph)paragrap.Clone();
                        pr.RemoveAllChildren<Word.Run>();
                        pr.RemoveAllChildren<Word.Break>();
                        pr.RemoveAllChildren<Word.SdtBlock>();
                        pr.RemoveAllChildren<Word.SdtRun>();

                        pr.Append(r);

                        paragrap.Parent.InsertAfter<Word.Paragraph>(pr, paragrap);
                        paragrap = pr;
                    }
                }
                if (p < pagesplit.Length - 1)
                {
                    Word.Break bp = new Word.Break();
                    bp.Type = Word.BreakValues.Page;
                    paragrap.AppendChild<Word.Break>(bp);
                }
            }
        }

        public Word.TableCell GenerateCell(string val, string w, int span = 0, bool bold = false, string sz = "12", string s = "style22", string f = "Courier New", Word.JustificationValues halign = Word.JustificationValues.Left)
        {
            Word.LeftMargin margin = new Word.LeftMargin { Type = new EnumValue<Word.TableWidthUnitValues>(Word.TableWidthUnitValues.Dxa), Width = new StringValue("10") };
            Word.TableCellWidth width = new Word.TableCellWidth { Type = new EnumValue<Word.TableWidthUnitValues>(Word.TableWidthUnitValues.Dxa), Width = w };
            Word.Shading shading = new Word.Shading { Fill = "auto", Val = new EnumValue<Word.ShadingPatternValues>(Word.ShadingPatternValues.Clear) };
            Word.TableCellMargin cellmargin = new Word.TableCellMargin(margin);
            Word.VerticalTextAlignmentOnPage align = new Word.VerticalTextAlignmentOnPage { Val = new EnumValue<Word.VerticalJustificationValues>(Word.VerticalJustificationValues.Center) };
            Word.GridSpan gspan = new Word.GridSpan { Val = span };

            Word.TableCellProperties props = new Word.TableCellProperties(width, shading, cellmargin, align, gspan);

            Word.Paragraph paragraph = GenerateParagraph(val, bold, sz, s, f, halign);

            Word.TableCell cell = new Word.TableCell(props, paragraph);
            return cell;
        }

        public Word.Paragraph GenerateParagraph(string val, bool bold = false, string sz = "12", string s = "style22", string f = "Courier New", Word.JustificationValues align = Word.JustificationValues.Left)
        {
            Word.ParagraphStyleId pstyle = new Word.ParagraphStyleId { Val = s };
            Word.Justification jut = new Word.Justification { Val = new EnumValue<Word.JustificationValues>(align) };
            Word.ParagraphProperties pprop = new Word.ParagraphProperties(pstyle, jut);

            Word.RunProperties rprop = new Word.RunProperties(
                new Word.RunFonts { Ascii = f, ComplexScript = f, HighAnsi = f },
                new Word.Bold { Val = new OnOffValue(bold) },
                new Word.BoldComplexScript { Val = new OnOffValue(bold) },
                new Word.FontSize { Val = sz });

            Word.Text text = new Word.Text(val);
            Word.Run run = new Word.Run(rprop, text);

            return new Word.Paragraph(pprop, run);
        }

        public void FindFields(OpenXmlElement documentPart, Type t, List<OpenXmlElement> results)
        {
            foreach (var child in documentPart.Elements())
            {
                if (child.GetType() == t)
                    results.Add(child);
                else
                    FindFields(child, t, results);
            }
        }

        private void Find<T>(OpenXmlElement documentPart, List<T> list) where T : OpenXmlElement
        {
            foreach (var child in documentPart)
            {
                if (child is T)
                    list.Add((T)child);
                else
                    Find<T>(child, list);
            }
        }

        private void FindFields(OpenXmlElement documentPart, Dictionary<Word.FieldChar, Word.FieldChar> results, ref Word.FieldChar lch)
        {
            foreach (var child in documentPart.Elements())
            {
                if (child is Word.FieldChar)
                {
                    Word.FieldChar fch = (Word.FieldChar)child;
                    if (fch.FieldCharType == Word.FieldCharValues.Begin)
                    {
                        lch = fch;
                        results.Add(fch, null);
                    }
                    else if (fch.FieldCharType == Word.FieldCharValues.End)
                        results[lch] = fch;
                }
                FindFields(child, results, ref lch);
            }
        }

    }


}