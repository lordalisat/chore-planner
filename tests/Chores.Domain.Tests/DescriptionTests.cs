using Xunit;

namespace Chores.Domain.Tests;

public class DescriptionTests
{
    private static OneOffChore NewChore() => new("Fix leaking tap", new DateOnly(2027, 1, 1));

    [Fact]
    public void A_Chore_has_no_Description_until_given_one()
    {
        Assert.Null(NewChore().Description);
    }

    [Fact]
    public void A_Description_keeps_its_line_breaks_but_not_the_blank_around_it()
    {
        OneOffChore chore = NewChore();

        chore.Description = "  Washer is in the drawer.\nTurn the water off first.\n ";

        Assert.Equal("Washer is in the drawer.\nTurn the water off first.", chore.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  \n ")]
    public void Only_blank_text_is_no_Description(string text)
    {
        OneOffChore chore = NewChore();

        chore.Description = text;

        Assert.Null(chore.Description);
    }

    [Fact]
    public void A_Description_holds_at_most_2000_characters()
    {
        OneOffChore chore = NewChore();

        chore.Description = new string('a', Chore.DescriptionMaxLength);
        Assert.Throws<ArgumentException>(() => chore.Description = new string('a', Chore.DescriptionMaxLength + 1));
        Assert.Equal(2000, Chore.DescriptionMaxLength);
    }
}
