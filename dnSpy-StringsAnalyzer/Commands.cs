using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;
using Plugin.StringAnalyzer;
using dnSpy.Contracts.MVVM;

namespace Plugin.StringAnalyzer
{
    public class Commands
    {
        private ICommand _copyMDToken;

        public ICommand CopyMDToken
        {
            get { return _copyMDToken ?? (_copyMDToken = new RelayCommand(p => copyMethodToken((string)p))); }
        }

        private void copyMethodToken(string item)
        {
            if (!string.IsNullOrEmpty(item))
            {
                //TODO
                MessageBox.Show("Hey you clicked an item!");
            }

        }
    }
}
