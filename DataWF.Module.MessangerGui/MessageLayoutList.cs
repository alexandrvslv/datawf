using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.ComponentModel;
using Xwt;
using Xwt.Drawing;
using DataWF.Module.Common;
using DataWF.Module.Messanger;

namespace DataWF.Module.MessangerGui
{
    public class MessageLayoutList : TableLayoutList
    {
        //private static PCellStyle StyleOut;
        //private static PCellStyle StyleIn;

        public MessageLayoutList()
            : base()
        {
            GenerateColumns = false;
            GenerateToString = false;

            var style = GuiEnvironment.Theme["MessageRow"];


            //var style = _listInfo.StyleCell.Clone();
            listInfo.StyleRow = style;

            listInfo.Indent = 8;
            listInfo.ColumnsVisible = false;
            listInfo.HotTrackingCell = false;

            listInfo.Columns.Add(new LayoutColumn() { Name = nameof(Message.DateCreate), Width = 120, Row = 0, Column = 0, Editable = false });
            listInfo.Columns.Add(new LayoutColumn() { Name = nameof(Message.User), Width = 120, Row = 0, Column = 0, Editable = false, FillWidth = true });
            listInfo.Columns.Add(new LayoutColumn() { Name = nameof(Message.Data), Width = 100, Row = 1, Column = 0, FillWidth = true });

            listInfo.Sorters.Add(new LayoutSort() { ColumnName = nameof(Message.DateCreate), IsGroup = true });
            listInfo.HeaderWidth = 20;

            //HighLight = false;
            ListInfo.CalcHeigh = true;
        }

        //protected override PCellStyle OnGetCellStyle(object listItem, object value, IPCell col)
        //{
        //    if (StyleOut == null)
        //    {
        //        StyleOut = new PCellStyle();
        //        StyleOut.Alternate = false;
        //        StyleOut.BorderBrush.Color = Color.FromArgb(200, Color.Green);
        //        StyleOut.BorderBrush.SColor = Color.FromArgb(255, Color.Green);
        //        StyleOut.BackBrush.Color = Color.FromArgb(40, Color.Green);
        //        StyleOut.BackBrush.SColor = Color.FromArgb(120, Color.Green);

        //        StyleIn = new PCellStyle();
        //        StyleIn.Alternate = false; 
        //        StyleIn.BorderBrush.Color = Color.FromArgb(200, Color.Orange);
        //        StyleIn.BorderBrush.SColor = Color.FromArgb(255, Color.Orange);
        //        StyleIn.BackBrush.Color = Color.FromArgb(40, Color.Orange);
        //        StyleIn.BackBrush.SColor = Color.FromArgb(120, Color.Orange);
        //    }
        //    PCellStyle pcs = base.OnGetCellStyle(listItem, value, col);
        //    if (col == null && listItem is Message)
        //    {
        //        if (((Message)listItem).User == FlowEnvir.Personal.User)
        //            pcs = StyleOut;
        //        else
        //            pcs = StyleIn;
        //    }
        //    return pcs;
        //}

        protected override void OnDrawHeader(LayoutListDrawArgs e)
        {
            var chatItem = e.Item as Message;
            var style = OnGetCellStyle(chatItem, null, null);
            e.Context.DrawGlyph(chatItem.User == User.CurrentUser ? GlyphType.SignOut : GlyphType.SignIn, e.Bound, style);
            //base.OnPaintHeader(context, index, dataSource, bound, state);
        }

        protected override void OnListChangedApp(object sender, EventArgs arg)
        {
            if (listSource.Count > 0)
                SelectedItem = listSource[listSource.Count - 1];
            base.OnListChangedApp(sender, arg);

        }
    }
}
