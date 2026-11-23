using Xunit;

namespace Chores.Domain.Tests;

public class ItemTests
{
    private static readonly DateOnly Today = new(2027, 3, 10);

    private static RecurringChore Bathroom(params string[] items)
    {
        var chore = new RecurringChore("Clean the bathroom", new Interval(1, IntervalUnit.Week), Today);
        chore.SetItems([.. items.Select(name => new Item(name))]);
        return chore;
    }

    [Fact]
    public void A_Chore_has_no_Items_until_given_some_and_keeps_their_order()
    {
        Assert.Empty(new OneOffChore("Fix leaking tap", Today).Items);

        RecurringChore chore = Bathroom("Sink", "Toilet", "Mirror");

        Assert.Equal(["Sink", "Toilet", "Mirror"], chore.Items.Select(i => i.Name));
    }

    [Fact]
    public void An_Item_has_a_trimmed_Name_and_a_Description_like_a_Chores()
    {
        var item = new Item("  Toilet ", "  Blue cleaner,\nleave it 10 minutes  ");

        Assert.Equal("Toilet", item.Name);
        Assert.Equal("Blue cleaner,\nleave it 10 minutes", item.Description);
        Assert.Null(new Item("Mirror", "  ").Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_Item_needs_a_Name(string name)
    {
        Assert.Throws<ArgumentException>(() => new Item(name));
    }

    [Fact]
    public void An_Items_Name_holds_at_most_200_characters_and_its_Description_2000()
    {
        _ = new Item(new string('a', Item.NameMaxLength));
        Assert.Throws<ArgumentException>(() => new Item(new string('a', Item.NameMaxLength + 1)));
        Assert.Throws<ArgumentException>(() => new Item("Mirror", new string('a', Chore.DescriptionMaxLength + 1)));
        Assert.Equal(200, Item.NameMaxLength);
    }

    [Fact]
    public void A_Chore_holds_at_most_20_Items()
    {
        RecurringChore chore = Bathroom();

        chore.SetItems([.. Enumerable.Range(1, Chore.MaxItems).Select(n => new Item($"Item {n}"))]);
        Assert.Throws<ArgumentException>(() => chore.SetItems([.. Enumerable.Range(1, Chore.MaxItems + 1).Select(n => new Item($"Item {n}"))]));
        Assert.Equal(20, Chore.MaxItems);
    }

    [Fact]
    public void Two_Items_of_one_Chore_cannot_share_a_Name()
    {
        RecurringChore chore = Bathroom("Sink");

        Assert.Throws<ArgumentException>(() => chore.SetItems([new Item("Sink"), new Item(" sink ")]));
        Assert.Equal(["Sink"], chore.Items.Select(i => i.Name));
    }

    [Fact]
    public void Ticking_records_no_Completion()
    {
        RecurringChore chore = Bathroom("Sink", "Toilet");

        chore.Items[0].Tick();

        Assert.True(chore.Items[0].Ticked);
        Assert.False(chore.Items[1].Ticked);
        Assert.Empty(chore.Completions);
        chore.Items[0].Untick();
        Assert.False(chore.Items[0].Ticked);
    }

    [Fact]
    public void Doing_or_skipping_a_Recurring_Chore_unticks_its_Items_and_undoing_does_not_tick_them_again()
    {
        RecurringChore chore = Bathroom("Sink", "Toilet");
        chore.Items[0].Tick();

        chore.Complete(Today);
        Assert.All(chore.Items, item => Assert.False(item.Ticked));

        chore.Items[1].Tick();
        chore.Skip(Today);
        Assert.All(chore.Items, item => Assert.False(item.Ticked));

        chore.RemoveCompletion(chore.Completions[^1]);
        Assert.All(chore.Items, item => Assert.False(item.Ticked));
    }

    [Fact]
    public void Postponing_keeps_the_ticks()
    {
        RecurringChore chore = Bathroom("Sink");
        chore.Items[0].Tick();

        chore.Postpone(Today.AddDays(3));

        Assert.True(chore.Items[0].Ticked);
    }

    [Fact]
    public void A_finished_One_off_Chore_keeps_its_ticks()
    {
        var shelf = new OneOffChore("Hang the shelf", Today);
        shelf.SetItems([new Item("Drill"), new Item("Plugs")]);
        shelf.Items[0].Tick();

        shelf.Complete(Today);

        Assert.True(shelf.Items[0].Ticked);
    }
}
