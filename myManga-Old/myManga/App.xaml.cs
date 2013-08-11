using System.Windows;
using myManga.ViewModels;
using System.Diagnostics;
using System;
using System.Reflection;
using System.Collections.Generic;
using BakaBox.DLL;

namespace myManga
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [DebuggerStepThrough]
    public partial class App : Application
    {
        private readonly EmbeddedDLLs _edll;
        public App()
        {
            _edll = new EmbeddedDLLs("Resources.DLLs");
            AppDomain.CurrentDomain.AssemblyResolve += _edll.ResolveAssembly;
            InitializeComponent();
        }
    }
}