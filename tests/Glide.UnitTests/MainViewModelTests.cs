using Glide.App.ViewModels;
using Glide.Core.Enums;
using Xunit;

namespace Glide.UnitTests;

public class MainViewModelTests
{
    [Fact]
    public void InitialState_ShouldBeHomeWithNormalGamingMode()
    {
        var vm = new MainViewModel();

        Assert.Equal(GlideModule.Home, vm.CurrentModule);
        Assert.False(vm.IsGamingMode);
        Assert.NotNull(vm.CurrentTrack);
        Assert.NotNull(vm.CurrentView);
        Assert.IsType<HomeViewModel>(vm.CurrentView);
        Assert.True(vm.IsHomeSelected);
    }

    [Theory]
    [InlineData(GlideModule.Audio, typeof(AudioViewModel))]
    [InlineData(GlideModule.Video, typeof(VideoViewModel))]
    [InlineData(GlideModule.Notifications, typeof(NotificationsViewModel))]
    [InlineData(GlideModule.Connect, typeof(ConnectViewModel))]
    [InlineData(GlideModule.Settings, typeof(SettingsViewModel))]
    public void NavigateCommand_ShouldSwitchModuleAndCurrentView(GlideModule target, Type expectedViewType)
    {
        var vm = new MainViewModel();

        vm.NavigateCommand.Execute(target);

        Assert.Equal(target, vm.CurrentModule);
        Assert.IsType(expectedViewType, vm.CurrentView);
    }

    [Fact]
    public void ToggleGamingMode_ShouldFlipState()
    {
        var vm = new MainViewModel();
        Assert.False(vm.IsGamingMode);

        vm.ToggleGamingModeCommand.Execute(null);
        Assert.True(vm.IsGamingMode);

        vm.ToggleGamingModeCommand.Execute(null);
        Assert.False(vm.IsGamingMode);
    }

    [Fact]
    public void TogglePlayPause_ShouldSwitchBetweenPlayingAndPaused()
    {
        var vm = new MainViewModel();
        Assert.Equal(AudioPlaybackState.Playing, vm.CurrentTrack.State);

        vm.TogglePlayPauseCommand.Execute(null);
        Assert.Equal(AudioPlaybackState.Paused, vm.CurrentTrack.State);

        vm.TogglePlayPauseCommand.Execute(null);
        Assert.Equal(AudioPlaybackState.Playing, vm.CurrentTrack.State);
    }

    [Theory]
    [InlineData("spotify", GlideModule.Audio)]
    [InlineData("google", GlideModule.Audio)]
    [InlineData("steam", GlideModule.Notifications)]
    [InlineData("instagram", GlideModule.Video)]
    [InlineData("twitter", GlideModule.Video)]
    public void HomeViewModel_ConnectAccount_ShouldNavigateToAppropriateModule(string serviceKey, GlideModule expectedModule)
    {
        var mainVm = new MainViewModel();
        var homeVm = new HomeViewModel(mainVm);

        homeVm.ConnectServiceCommand.Execute(serviceKey);

        Assert.Equal(expectedModule, mainVm.CurrentModule);
    }
}

