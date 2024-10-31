using EscapePod.ViewModels;
using FluentAssertions;
using Xunit;

namespace EscapePod.Tests
{
    public class MainWindowViewModelTests
    {
        [Fact]
        public void ConstructorTest1() 
        { 
            MainWindowViewModel viewModel = new MainWindowViewModel();
            viewModel.Should().NotBeNull();
        }
    }
}
