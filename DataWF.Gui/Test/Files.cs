﻿using DataWF.Common;
using DataWF.Gui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xwt;

namespace DataWF.TestGui
{
    public class Files : VPanel
    {
        private LayoutList directoryTree = new LayoutList();
        private LayoutList directoryView = new LayoutList();
        private HPaned split = new HPaned();
        private HBox status = new HBox();
        private Label statusLablel = new Label();

        private DirectoryNode current;
        private Queue<DirectoryNode> actions = new Queue<DirectoryNode>();
        private SelectableList<FileItem> files = new SelectableList<FileItem>();
        private EventWaitHandle flag = new EventWaitHandle(true, EventResetMode.ManualReset);

        public Files()
        {
            split.Name = "splitContainer1";

            directoryTree.AllowCellSize = true;
            directoryTree.Mode = LayoutListMode.Tree;
            directoryTree.Name = "dTree";
            directoryTree.Text = "Directory Tree";
            directoryTree.SelectionChanged += DTreeSelectionChanged;
            directoryTree.Nodes.ItemPropertyChanged += NodesItemPropertyChanged;

            directoryView.Mode = LayoutListMode.List;
            directoryView.Name = "flist";
            directoryView.Text = "Directory";
            directoryView.CellDoubleClick += FListCellDoubleClick;
            directoryView.ListSource = files;

            status.Name = "status";
            status.PackStart(statusLablel);

            split.Panel1.Content = directoryTree;
            split.Panel2.Content = directoryView;

            PackStart(split, true, true);
            PackStart(status, false, false);
            Text = "Files";

            var task = new Task(CheckQueue, TaskCreationOptions.LongRunning);
            task.Start();
            GuiService.Localize(this, "Files", "Files", GlyphType.FilesO);

            Task.Run(() =>
                {
                    var drives = DriveInfo.GetDrives();
                    foreach (var drive in drives)
                    {
                        try
                        {
                            directoryTree.Nodes.Add(InitDrive(drive));
                        }
                        catch (Exception e)
                        {
                            Helper.OnException(e);
                        }
                    }
                }).ConfigureAwait(false);

        }

        private DirectoryNode InitDrive(DriveInfo drive)
        {
            var node = InitDirectory(drive.RootDirectory);
            node.Drive = drive;
            node.Text = string.Format("{0} {1}", drive.Name, drive.VolumeLabel);
            if (drive.DriveType == DriveType.Fixed)
                node.Glyph = Locale.GetGlyph(drive.DriveType.GetType().FullName, drive.DriveType.ToString(), GlyphType.HddO);
            else if (drive.DriveType == DriveType.CDRom)
                node.Glyph = Locale.GetGlyph(drive.DriveType.GetType().FullName, drive.DriveType.ToString(), GlyphType.Desktop);
            else
                node.Glyph = Locale.GetGlyph(drive.DriveType.GetType().FullName, drive.DriveType.ToString(), GlyphType.FloppyO);
            CheckSubDirectory(node);
            return node;
        }

        private DirectoryNode InitDirectory(DirectoryInfo directory)
        {
            var node = (DirectoryNode)directoryTree.Nodes.Find(directory.FullName);
            if (node == null)
                node = new DirectoryNode()
                {
                    Name = directory.FullName,
                    Text = directory.Name,
                    Glyph = Locale.GetGlyph("Files", "Directory", GlyphType.Folder),
                    File = new FileItem() { Info = directory }
                };

            return node;
        }

        private void CheckSubDirectory(DirectoryNode node)
        {
            if (!actions.Contains(node))
            {
                actions.Enqueue(node);
                flag.Set();
            }
        }

        private void CheckQueue()
        {
            while (true)
            {
                flag.WaitOne();
                while (actions.Count > 0)
                {
                    var node = actions.Dequeue();
                    if (node != null)
                    {
                        Application.Invoke(() =>
                        {
                            statusLablel.Text = string.Format("Check: {0}", node.Name);
                        });

                        CheckNode(node);
                        if (node == Current)
                        {
                            files.Clear();
                            files.AddRange(node.Nodes.OfType<DirectoryNode>().Select(p => p.File));

                            var directory = (DirectoryInfo)node.File.Info;
                            files.AddRange(directory.GetFiles().Select(p => new FileItem() { Info = p }));
                        }
                        for (int i = 0; i < node.Nodes.Count; i++)
                        {
                            var check = (DirectoryNode)node.Nodes[i];
                            CheckNode(check);
                        }

                    }

                    Application.Invoke(() =>
                    {
                        statusLablel.Text += string.Format("      Nodes: {0} Queue: {1}",
                                                          directoryTree.Nodes.Count,
                                                         actions.Count);
                    });
                }
                flag.Reset();
            }
        }

        private void CheckNode(DirectoryNode check)
        {
            if (!check.Check)
            {
                check.Check = true;
                var directory = (DirectoryInfo)check.File.Info;
                try
                {
                    var directories = directory.GetDirectories();
                    foreach (var item in directories)
                    {
                        if ((item.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden &&
                                                 (item.Attributes & FileAttributes.System) != FileAttributes.System)
                        {
                            Node snode = InitDirectory(item);
                            snode.Group = check;
                        }
                    }
                }
                catch (Exception ex)
                {
                    check.Text += ex.Message;
                }
            }
        }


        public DirectoryNode Current
        {
            get { return current; }
            set
            {
                if (current != value)
                {
                    current = value;
                    if (current != null)
                    {
                        var drive = current.Group == null ? current.Drive : null;
                        var text = drive == null ? current.File.Info.FullName : string.Format("{0} free {1} of {2}", current.File.Info.FullName,
                                       Helper.LenghtFormat(drive.TotalFreeSpace),
                                       Helper.LenghtFormat(drive.TotalSize));
                        statusLablel.Text = text;

                        CheckSubDirectory(current);
                    }
                }
            }
        }

        private void NodesItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Node.Expand))
            {
                var node = (DirectoryNode)sender;
                if (node.Drive == null)
                {
                    node.Glyph = node.Expand ? Locale.GetGlyph("Files", "DirectoryOpen", GlyphType.FolderOpen) :
                        Locale.GetGlyph("Files", "Directory", GlyphType.Folder);
                }
            }
        }

        private void DTreeSelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            if (e.Value is LayoutSelectionRow && e.Type != LayoutSelectionChange.Remove && e.Type != LayoutSelectionChange.Hover)
                Current = ((LayoutSelectionRow)e.Value).Item as DirectoryNode;
        }

        private void FListCellDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            var item = (FileItem)e.HitTest.Item;
            if (item.Info is DirectoryInfo)
            {
                SelectNode(item);
            }
        }

        private void FListKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && directoryView.SelectedItem != null)
            {
                var item = (FileItem)directoryView.SelectedItem;
                if (item.Info is DirectoryInfo)
                {
                    SelectNode(item);
                }
            }
            if (e.Key == Key.BackSpace && Current != null && Current.Group != null)
            {
                SelectNode((FileItem)Current.Group.Tag);
            }
            if (e.Key == Key.F && e.Modifiers == ModifierKeys.Control && directoryView.CurrentCell != null)
            {
                directoryView.AddFilter(directoryView.CurrentCell, true);
            }
        }

        private void SelectNode(FileItem item)
        {
            var node = InitDirectory((DirectoryInfo)item.Info);
            directoryTree.SelectedNode = node;
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }
    }

    public class DirectoryNode : Node
    {
        public DriveInfo Drive { get; set; }
        public FileItem File { get; set; }
    }
}
