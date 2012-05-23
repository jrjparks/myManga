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
        public App()
        {
            EmbeddedDLLs.Instance.SetEmbeddedDLLResourceLocation("Resources.DLLs");
            AppDomain.CurrentDomain.AssemblyResolve += EmbeddedDLLs.Instance.ResolveAssembly;
            InitializeComponent();
        }
    }
}