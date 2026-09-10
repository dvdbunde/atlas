using ATLAS.Application.Queries.Admin;
using ATLAS.Blazor.ViewModels;
using Xunit;

namespace ATLAS.Blazor.Tests.ViewModels;

public class AdminApplicationExplorerViewModelTests
{
    [Fact]
    public void DefaultSort_ShouldBeLastUpdated()
    {
        // Arrange & Act
        var vm = new AdminApplicationExplorerViewModel();

        // Assert — M12-010: default sort is Updated / Last Updated
        Assert.Equal(AdminApplicationSortBy.LastUpdated, vm.SortBy);
    }
}
