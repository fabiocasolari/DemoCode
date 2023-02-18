﻿using DigiMixer.Mackie;
using DigiMixer.Osc;
using DigiMixer.QuSeries;
using DigiMixer.UCNet;
using DigiMixer.UiHttp;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace DigiMixer.Wpf;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void LaunchUi24R(object sender, RoutedEventArgs e)
    {
        var api = new UiHttpMixerApi(CreateLogger("Ui24r"), "192.168.1.57");        
        var mixer = await Mixer.Detect(api);
        Launch(mixer);
    }

    private async void LaunchXR18(object sender, RoutedEventArgs e)
    {
        var api = XAir.CreateMixerApi(CreateLogger("XR18"), "192.168.1.41");
        var mixer = await Mixer.Detect(api);
        Launch(mixer);
    }

    private async void LaunchX32(object sender, RoutedEventArgs e)
    {
        var api = X32.CreateMixerApi(CreateLogger("X32"), "192.168.1.62");
        var mixer = await Mixer.Detect(api);
        Launch(mixer);
    }

    private async void LaunchXR16(object sender, RoutedEventArgs e)
    {
        var api = XAir.CreateMixerApi(CreateLogger("XR16"), "192.168.1.185");
        var mixer = await Mixer.Detect(api);
        Launch(mixer);
    }

    private async void LaunchM18(object sender, RoutedEventArgs e)
    {
        var api = Rcf.CreateMixerApi(CreateLogger("M18"), "192.168.1.58");
        var mixer = await Mixer.Detect(api);
        Launch(mixer);
    }

    private async void LaunchDL16S(object sender, RoutedEventArgs e)
    {
        var api = new MackieMixerApi(CreateLogger("DL16S"), "192.168.1.59");
        var mixer = await Mixer.Detect(api);
        Launch(mixer);
    }

    private async void Launch16R(object sender, RoutedEventArgs e)
    {
        var api = StudioLive.CreateMixerApi(CreateLogger("16R"), "192.168.1.61");
        var mixer = await Mixer.Detect(api);
        Launch(mixer);
    }

    private async void LaunchQuSB(object sender, RoutedEventArgs e)
    {
        var api = QuMixer.CreateMixerApi(CreateLogger("Qu-SB"), "192.168.1.60");
        var mixer = await Mixer.Detect(api);
        Launch(mixer);
    }

    private void Launch(Mixer mixer)
    {
        var vm = new MixerViewModel(mixer);        
        var window = new MixerWindow { DataContext = vm };
        window.Show();
    }

    private static ILogger CreateLogger(string name) => App.Current.Log.CreateLogger(name);
}
