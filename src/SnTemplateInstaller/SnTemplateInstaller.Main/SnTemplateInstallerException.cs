using System;
using System.Collections.Generic;
using System.Text;

namespace SnTemplateInstaller.Main
{
    //UNDONE: finish exception class
    public class SnTemplateInstallerException : Exception
    {
        public SnTemplateInstallerException(string message) : base(message)
        {
        }
    }
}
