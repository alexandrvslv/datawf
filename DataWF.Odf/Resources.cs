using System;
using System.IO;

namespace Doc.Odf
{
	public class Resources
	{
		private static byte[] emptyODT;
		public static byte[] EmptyODT {
			get {
				if (emptyODT == null) {
					using (var stream = typeof(Resources).Assembly.GetManifestResourceStream ("doc.odf.Resources.empty.odt")) {
						emptyODT = new byte[stream.Length];
						stream.Read (emptyODT, 0, emptyODT.Length);
					}
				}
				return emptyODT;
			}
		}
        private static byte[] emptyODS;
        public static byte[] EmptyODS
        {
            get
            {
                if (emptyODS == null)
                {
                    using (var stream = typeof(Resources).Assembly.GetManifestResourceStream("doc.odf.Resources.empty.ods"))
                    {
                        emptyODS = new byte[stream.Length];
                        stream.Read(emptyODS, 0, emptyODS.Length);
                    }
                }
                return emptyODS;
            }
        }
	}
	
}


