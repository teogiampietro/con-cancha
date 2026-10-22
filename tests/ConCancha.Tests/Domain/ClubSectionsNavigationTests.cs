extern alias web;

using ConCancha.Domain.Clubs;
using web::ConCancha.Web.Services;
using web::ConCancha.Web.Services.Navigation;

namespace ConCancha.Tests.Domain;

public class ClubSectionsNavigationTests
{
    private static readonly string Owner = nameof(ClubRole.Owner);

    private static readonly IReadOnlyList<PanelNavItem> Items =
    [
        new("Agenda", "/agenda", "cal", PanelNavGroup.Today),
        new("Buffet", "/buffet", "box", PanelNavGroup.Club, Module: ClubModules.Buffet),
        new("Torneos", "/torneos", "trophy", PanelNavGroup.Club, Module: ClubModules.Tournaments),
    ];

    private static IReadOnlyList<string> Routes(ClubModules modules) =>
        PanelNavItems.Filter(Items, Owner, modules).Select(item => item.Route).ToList();

    [Fact]
    public void WithEverySectionOn_AllItemsShow() =>
        Assert.Equal(["/agenda", "/buffet", "/torneos"], Routes(ClubModules.All));

    [Fact]
    public void HidingBuffet_RemovesOnlyBuffet() =>
        Assert.Equal(["/agenda", "/torneos"], Routes(ClubModules.Tournaments));

    [Fact]
    public void HidingEverySection_KeepsTheItemsThatAreNotOptional() =>
        Assert.Equal(["/agenda"], Routes(ClubModules.None));

    [Fact]
    public void HiddenSection_StaysHiddenEvenForTheOwner() =>
        Assert.DoesNotContain("/buffet", Routes(ClubModules.Tournaments));

    [Fact]
    public void TheCurrentCatalog_IsTheSameWhateverTheSections()
    {
        var all = PanelNavItems.VisibleTo(Owner, ClubModules.All).Select(item => item.Route);
        var none = PanelNavItems.VisibleTo(Owner, ClubModules.None).Select(item => item.Route);

        Assert.Equal(all, none);
    }
}

public class ClubThemeTests
{
    [Fact]
    public void Style_SetsAccentTextAndSecondaryVariables() =>
        Assert.Equal("--acc:#B8341E;--onacc:#FFFFFF;--club2:#3F7D5A", ClubTheme.Style("#b8341e", "#3f7d5a"));

    [Theory]
    [InlineData("red; background: url(//evil)")]
    [InlineData("#0B1020")]
    [InlineData("")]
    public void Style_NeverEmitsAnUnsafeOrUnreadableColor(string color) =>
        Assert.StartsWith($"--acc:{ClubBranding.DefaultPrimaryColor};", ClubTheme.Style(color, ClubBranding.DefaultSecondaryColor));
}
