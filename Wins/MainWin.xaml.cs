﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using OnaCore;
using Sheas_Cealer.Consts;
using Sheas_Cealer.Preses;
using Sheas_Cealer.Utils;
using File = System.IO.File;

namespace Sheas_Cealer.Wins;

public partial class MainWin : Window
{
    private static string CealArgs = string.Empty;
    private static readonly HttpClient MainClient = new();
    private static DispatcherTimer HoldButtonTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private static readonly FileSystemWatcher CealingHostWatcher = new(AppDomain.CurrentDomain.SetupInformation.ApplicationBase!, "Cealing-Host.json") { EnableRaisingEvents = true, NotifyFilter = NotifyFilters.LastWrite };
    private static MainPres? MainPres;

    internal MainWin(string[] args)
    {
        InitializeComponent();

        DataContext = MainPres = new(args);
        CealingHostWatcher.Changed += CealingHostWatcher_Changed;
        CealingHostWatcher_Changed(null!, null!);
    }
    protected override void OnSourceInitialized(EventArgs e) => IconRemover.RemoveIcon(this);
    private void MainWin_Loaded(object sender, RoutedEventArgs e) => SettingsBox.Focus();
    private void MainWin_Closing(object sender, CancelEventArgs e) => Application.Current.Shutdown();

    private void MainWin_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None;
        e.Handled = true;
    }
    private void MainWin_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            MainPres!.BrowserPath = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
    }

    private void SettingsBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        switch (MainPres!.Mode)
        {
            case MainConst.SettingsMode.BrowserPathMode:
                MainPres.BrowserPath = SettingsBox.Text;
                return;
            case MainConst.SettingsMode.UpstreamUrlMode:
                MainPres.UpstreamUrl = SettingsBox.Text;
                return;
            case MainConst.SettingsMode.ExtraArgsMode:
                MainPres.ExtraArgs = SettingsBox.Text;
                return;
        }
    }
    private void SettingsFunctionButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog browserPathDialog = new() { Filter = "浏览器 (*.exe)|*.exe" };

        switch (MainPres!.Mode)
        {
            case MainConst.SettingsMode.BrowserPathMode when browserPathDialog.ShowDialog().GetValueOrDefault():
                SettingsBox.Focus();
                MainPres.BrowserPath = browserPathDialog.FileName;
                return;
            case MainConst.SettingsMode.UpstreamUrlMode:
                MainPres.UpstreamUrl = MainConst.DefaultUpstreamUrl;
                return;
            case MainConst.SettingsMode.ExtraArgsMode:
                MainPres.ExtraArgs = string.Empty;
                return;
        }
    }
    private void SettingsModeButton_Click(object sender, RoutedEventArgs e)
    {
        MainPres!.Mode = MainPres.Mode switch
        {
            MainConst.SettingsMode.BrowserPathMode => MainConst.SettingsMode.UpstreamUrlMode,
            MainConst.SettingsMode.UpstreamUrlMode => MainConst.SettingsMode.ExtraArgsMode,
            MainConst.SettingsMode.ExtraArgsMode => MainConst.SettingsMode.BrowserPathMode,
            _ => throw new UnreachableException()
        };
    }

    internal void StartCealButton_Click(object sender, RoutedEventArgs e)
    {
        if (HoldButtonTimer.IsEnabled)
            HoldButtonTimer.Stop();
        else
            return;

        if (string.IsNullOrWhiteSpace(CealArgs))
            throw new Exception("规则无法识别，请检查伪造规则是否含有语法错误");
        if (MessageBox.Show("启动前将关闭所选浏览器的所有进程，是否继续？", string.Empty, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;

        IWshShortcut uncealedBrowserShortcut = (IWshShortcut)new WshShell().CreateShortcut(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase!, @"Uncealed-Browser.lnk"));
        uncealedBrowserShortcut.TargetPath = MainPres!.BrowserPath;
        uncealedBrowserShortcut.Description = "Created By Sheas Cealer";
        uncealedBrowserShortcut.Save();

        foreach (Process browserProcess in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(MainPres.BrowserPath)))
        {
            browserProcess.Kill();
            browserProcess.WaitForExit();
        }

        new CommandProc(true).ShellRun(AppDomain.CurrentDomain.SetupInformation.ApplicationBase!, (CealArgs + " " + MainPres!.ExtraArgs).Trim());
    }
    private void StartCealButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        HoldButtonTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        HoldButtonTimer.Tick += StartCealButtonHoldButtonTimer_Tick;
        HoldButtonTimer.Start();
    }
    private void StartCealButtonHoldButtonTimer_Tick(object? sender, EventArgs e)
    {
        HoldButtonTimer.Stop();

        if (HoldButtonTimer.IsEnabled)
        {
            HoldButtonTimer.Stop();
            return;
        }

        if (string.IsNullOrWhiteSpace(CealArgs))
            throw new Exception("规则无法识别，请检查伪造规则是否含有语法错误");
        if (MessageBox.Show("启动前将关闭所选浏览器的所有进程，是否继续？", string.Empty, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;

        IWshShortcut uncealedBrowserShortcut = (IWshShortcut)new WshShell().CreateShortcut(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase!, @"Uncealed-Browser.lnk"));
        uncealedBrowserShortcut.TargetPath = MainPres!.BrowserPath;
        uncealedBrowserShortcut.Description = "Created By Sheas Cealer";
        uncealedBrowserShortcut.Save();

        foreach (Process browserProcess in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(MainPres.BrowserPath)))
        {
            browserProcess.Kill();
            browserProcess.WaitForExit();
        }

        if (string.IsNullOrWhiteSpace(MainPres.ExtraArgs))
            MainPres.ExtraArgs = " " + MainPres.ExtraArgs;

        new CommandProc(false).ShellRun(AppDomain.CurrentDomain.SetupInformation.ApplicationBase!, CealArgs + (string.IsNullOrWhiteSpace(MainPres.ExtraArgs) ? "" : " " + MainPres.ExtraArgs));
    }

    private void EditHostButton_Click(object sender, RoutedEventArgs e)
    {
        ProcessStartInfo processStartInfo = new(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase!, @"Cealing-Host.json")) { UseShellExecute = true };
        Process.Start(processStartInfo);
    }
    private async void UpdateHostButton_Click(object sender, RoutedEventArgs e)
    {
        string hostUrl = MainPres!.UpstreamUrl;
        string UpdateHostString = await Http.GetAsync<string>(hostUrl, MainClient);
        string hostLocalString;
        using (StreamReader hostLocalStreamReader = new(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase!, @"Cealing-Host.json")))
            hostLocalString = hostLocalStreamReader.ReadToEnd();

        if (hostLocalString.Replace("\r", string.Empty) == UpdateHostString)
            MessageBox.Show("本地伪造规则和上游一模一样");
        else
        {
            MessageBoxResult overrideResult = MessageBox.Show("本地伪造规则和上游略有不同，需要覆盖本地吗? 否则只为你打开上游规则的网页", string.Empty, MessageBoxButton.YesNoCancel);
            if (overrideResult == MessageBoxResult.Yes)
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase!, @"Cealing-Host.json"), UpdateHostString);
                MessageBox.Show("更新已完成");
            }
            else if (overrideResult == MessageBoxResult.No)
                Process.Start(new ProcessStartInfo(hostUrl) { UseShellExecute = true });
        }
    }
    private void ThemesButton_Click(object sender, RoutedEventArgs e) => MainPres!.IsLightTheme = MainPres.IsLightTheme.HasValue ? (MainPres.IsLightTheme.Value ? null : true) : false;
    private void AboutButton_Click(object sender, RoutedEventArgs e) => new AboutWin().ShowDialog();

    internal void CealingHostWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        try
        {
            string hostRules = string.Empty, hostResolverRules = string.Empty;
            int ruleIndex = 0;
            using FileStream hostStream = new(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase!, @"Cealing-Host.json"), FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using StreamReader hostReader = new(hostStream);
            JArray hostJArray = JArray.Parse(hostReader.ReadToEnd());

            foreach (var hostJToken in hostJArray)
            {
                if (string.IsNullOrWhiteSpace(hostJToken[1]!.ToString()))
                    hostJToken[1] = "c" + ruleIndex;

                foreach (var hostName in hostJToken[0]!)
                    hostRules += "MAP " + hostName + " " + hostJToken[1] + ",";

                hostResolverRules += "MAP " + hostJToken[1] + " " + hostJToken[2] + ",";

                ++ruleIndex;
            }


            CealArgs = @$"/c @start .\""Uncealed-Browser.lnk"" --host-rules=""{hostRules[0..^1]}"" --host-resolver-rules=""{hostResolverRules[0..^1]}"" --test-type --ignore-certificate-errors";
        }
        catch { CealArgs = string.Empty; }
    }
    private void MainWin_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.W)
            Application.Current.Shutdown();
    }
}