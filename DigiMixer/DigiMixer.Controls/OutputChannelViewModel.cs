﻿using JonSkeet.WpfUtil;
using DigiMixer;
using DigiMixer.Core;

namespace DigiMixer.Controls;

// Note: we currently create multiple OutputChannelViewModels for the same output:
// one with faders-per-input and one without. That may not be ideal.
public class OutputChannelViewModel : ChannelViewModelBase<OutputChannel>
{
    public FaderViewModel OverallFader => Faders[0];

    /// <summary>
    /// Whether this output is a foldback.
    /// </summary>
    public bool IsFoldback { get; }

    public OutputChannelViewModel(ChannelMapping mapping)
        : base(ChannelId.Output(mapping.Channel), mapping)
    {
        IsFoldback = mapping.Foldback;
    }

    internal void SetFaders(IEnumerable<InputChannelViewModel> inputChannels)
    {
        Faders = new[] { new FaderViewModel(inputChannelId: null, ChannelId, inputId: null, Id, ChannelAppearance.CreateVisibleWhite()) }
            .Concat(inputChannels.Select(input => new FaderViewModel(input.ChannelId, ChannelId, input.Id, Id, input.Appearance)))
            .ToReadOnlyList();
    }

    protected override IEnumerable<OutputChannel> GetChannels(Mixer mixer) => mixer.OutputChannels;
}
