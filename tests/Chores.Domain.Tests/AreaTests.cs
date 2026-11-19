using Xunit;

namespace Chores.Domain.Tests;

public class AreaTests
{
    [Fact]
    public void An_Area_can_sit_inside_another()
    {
        var downstairs = new Area("Downstairs");
        var kitchen = new Area("Kitchen");

        kitchen.Parent = downstairs;

        Assert.Same(downstairs, kitchen.Parent);
    }

    [Fact]
    public void An_Area_cannot_be_its_own_ancestor()
    {
        var house = new Area("House");
        var downstairs = new Area("Downstairs", house);
        var kitchen = new Area("Kitchen", downstairs);

        Assert.Throws<ArgumentException>(() => house.Parent = kitchen);
        Assert.Throws<ArgumentException>(() => house.Parent = house);
        Assert.Null(house.Parent);
    }
}
