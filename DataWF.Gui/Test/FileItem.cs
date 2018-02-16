using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt;
using Xwt.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataWF.Gui;

namespace DataWF.TestGui
{
    public class FileItem : IGlyph
    {
        private FileSystemInfo info;
        private FileInfo finfo;
        private Image image;
        private GlyphType glyph;

        public override string ToString()
        {
            return info.Name;
        }

        [Browsable(false)]
        public FileSystemInfo Info
        {
            get { return info; }
            set
            {
                info = value;
                if (info is DirectoryInfo)
                {
                    glyph = Locale.GetGlyph("Files", "Directory", GlyphType.Folder);
                }
                else if (info is FileInfo)
                {
                    finfo = (FileInfo)info;
                    var ext = finfo.Extension.ToLower();
                    if (ext == ".pdf")
                        glyph = GlyphType.FilePdfO;
                    else if (ext == ".txt" || ext == ".rtf")
                        glyph = GlyphType.FileTextO;
                    else if (ext == ".avi" || ext == ".mkv")
                        glyph = GlyphType.FileVideoO;
                    else if (ext == ".doc" || ext == ".docx")
                        glyph = GlyphType.FileWordO;
                    else if (ext == ".zip" || ext == ".7z")
                        glyph = GlyphType.FileZipOAlias;
                    else if (ext == ".mp3" || ext == ".ogg")
                        glyph = GlyphType.FileAudioO;
                    else if (ext == ".png" || ext == ".jpg")
                        glyph = GlyphType.FileImageO;
                    else if (ext == ".xls" || ext == ".xlsx")
                        glyph = GlyphType.FileExcelO;
                    else if (ext == ".cs" || ext == ".cpp" || ext == ".h")
                        glyph = GlyphType.FileCodeO;
                    else if (ext == ".rar" || ext == ".cab")
                        glyph = GlyphType.FileArchiveO;
                    else if (ext == ".fon" || ext == ".ttf")
                        glyph = GlyphType.Font;
                    else if (ext == ".exe" || ext == ".run")
                        glyph = GlyphType.GearAlias;
                    else if (ext == ".dll" || ext == ".o")
                        glyph = GlyphType.GearsAlias;
                    else
                        glyph = Locale.GetGlyph("Files", "File", GlyphType.FileO);
                }
            }
        }

        public long Size
        {
            get { return finfo != null ? finfo.Length : 0; }
        }

        public string Extension
        {
            get { return finfo != null ? finfo.Extension : null; }
        }

        public DateTime CreationTime
        {
            get { return info.CreationTime; }
        }

        public DateTime LastWriteTime
        {
            get { return info.LastWriteTime; }
        }

        public string FullName
        {
            get { return info.FullName; }
        }

        public FileAttributes Attributes
        {
            get { return info.Attributes; }
        }

        public bool IsDirrectory
        {
            get { return info is DirectoryInfo; }
        }

        [Browsable(false)]
        public Image Image
        {
            get { return image; }
            set { image = value as Image; }
        }

        [Browsable(false)]
        public GlyphType Glyph
        {
            get { return glyph; }
            set { glyph = value; }
        }
    }
}
