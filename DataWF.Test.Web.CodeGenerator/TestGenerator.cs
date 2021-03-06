﻿using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using DataWF.Module.Flow;
using DataWF.Module.Messanger;
using DataWF.WebService.Generator;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace DataWF.Test.Web.CodeGenerator
{
    [TestFixture()]
    public class TestGenerator
    {
        [Test()]
        public void GenerateCode()
        {
            var generator = new DataWF.WebService.Generator.ServiceGenerator(
                new[] {
                    typeof(Company).Assembly,
                    typeof(User).Assembly,
                    typeof(Message).Assembly,
                    typeof(Document).Assembly
                }, "Controllers", "Test.Code.Generator")
            {
                Mode = CodeGeneratorMode.Controllers | CodeGeneratorMode.Invokers | CodeGeneratorMode.Logs
            };
            generator.Generate();
            generator.Compile();

        }
    }
}
