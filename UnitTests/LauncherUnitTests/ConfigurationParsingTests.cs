using Divooka.Core.Helpers;
using Launcher.Shared;

namespace LauncherUnitTests
{
    public class ConfigurationParsingTests
    {
        [Fact]
        public void ShouldBeAbleToRecognizeVariousPatterns()
        {
            Assert.Equal(LaunchType.Verbatim, LauncherCore.ParseShortcut(@"workspace: !code ""C:\My Folder\My Subfolder""").Type);
            Assert.Equal(LaunchType.DiskLocation, LauncherCore.ParseShortcut(@"folder: C:\My Folder\My Subfolder").Type);
        }
    }
}