using System;
using System.Collections.Generic;
using System.Text;

namespace SnTemplateInstaller.Main
{
    public class InstallerEventArgs : EventArgs
    {
        public string Task { get; internal set; }
        public TimeSpan Elapsed { get; internal set; } = TimeSpan.Zero;
        public TimeSpan ElapsedFull { get; internal set; } = TimeSpan.Zero;
    }
}
