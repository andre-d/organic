using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Organic.Plugins;
using Organic;

namespace TestPlugin
{
    public class Plugin : IPlugin
    {
        #region Cow

        string cow = @"                   ________________________
          (__)    /                        \         
          (oo)   (      Organic Rocks!      )
   /-------\/  --'\________________________/        
  / |     ||
 *  ||----||
    ^^    ^^";

        #endregion

        #region Metadata

        public string Name
        {
            get { return "testplugin"; } // No spaces, all lowercase
        }

        public string Description
        {
            get { return "A test plugin for Organic"; }
        }

        public Version Version
        {
            get { return new Version(1, 0); }
        }

        #endregion

        Assembler assembler;

        public void Loaded(Assembler assembler)
        {
            this.assembler = assembler;
            assembler.AddHelpEntry("TestPlugin:\n" +
                "\t--cow: Output a cow to the console.");
            assembler.TryHandleParameter += new EventHandler<HandleParameterEventArgs>(assembler_TryHandleParameter);
            assembler.EvaluateExpressionValue += new EventHandler<EvaluateValueEventArgs>(assembler_EvaluateExpressionValue);
        }

        void assembler_EvaluateExpressionValue(object sender, EvaluateValueEventArgs e)
        {
            if (e.Value.StartsWith("#"))
            {
                e.Result = 0x1234;
                e.Handled = true;
            }
        }

        void assembler_TryHandleParameter(object sender, HandleParameterEventArgs e)
        {
            if (e.Parameter == "--cow")
            {
                Console.WriteLine(cow);
                e.Handled = true;
                e.StopProgram = true;
            }
        }
    }
}
