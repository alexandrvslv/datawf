using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Test.Web.CodeGenerator
{
    public partial class WebClientTestProvider : ClientProviderBase
    {
        public WebClientTestProvider()
        {
            InitializeComponent();
        }

        public WebClientTestProvider(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }
    }
}
