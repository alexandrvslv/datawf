using DataWF.Common;
using DataWF.Gui;
using Doc.Odf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
//using System.Windows.Forms;

namespace DataWF.Data.Gui
{
    public static class DataCtrlService
    {

        public static void ExportFList(LayoutList fields)
        {
            if (fields.FieldSource == null)
                return;
            TextDocument td = new TextDocument();

            var ps = new ParagraphStyle(td);
            ps.TextProperties.FontSize = "14";
            var pr = new Paragraph(td);
            pr.Style = ps;
            td.BodyText.Add(pr);
            pr.Add(fields.Name + "\n" + fields.FieldSource.ToString() + "\n\n");

            ps = new ParagraphStyle(td);
            ps.TextProperties.FontSize = "10";
            pr = new Paragraph(td);
            pr.Style = ps;
            td.BodyText.Add(pr);
            pr.Add(new Placeholder(td, "таблица", PlaceholdeType.Table));

            var elements = new Dictionary<string, object>();

            DateTime dt = DateTime.Now;

            elements.Add("дата", dt.ToString("D", Locale.Instance.Culture));

            string filename = Path.Combine(Helper.GetDirectory(Environment.SpecialFolder.LocalApplicationData), fields.FieldSource.ToString() + DateTime.Now.ToString("yyyyMMddHHmss") + ".odt");
            // File.WriteAllBytes(filename, td.UnLoad());

            var subparam = new Dictionary<string, object>();
            var prms = new List<Dictionary<string, object>>();

            foreach (LayoutField field in fields.Fields)
            {
                if (!field.Visible)
                    continue;
                subparam = new Dictionary<string, object>
                {
                    { "header", field.Text },
                    { "value", field.ReadValue(fields.FieldSource) }
                };
                prms.Add(subparam);
            }
            elements.Add("таблица", prms);

            var op = new OdtProcessor(td);
            op.PerformReplace(elements);

            td.Save(filename);
            Process.Start(filename);
        }

    }
}

