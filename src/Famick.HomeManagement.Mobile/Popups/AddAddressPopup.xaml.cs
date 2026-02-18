using CommunityToolkit.Maui.Views;
using Famick.HomeManagement.Mobile.Models;

namespace Famick.HomeManagement.Mobile.Popups;

public partial class AddAddressPopup : Popup<AddAddressResult>
{
    private static readonly int[] TagValues = { 0, 1, 2, 3, 4, 99 };

    public AddAddressPopup()
    {
        InitializeComponent();
        TagPicker.SelectedIndex = 0;
    }

    public AddAddressPopup(ContactAddressDto existing) : this()
    {
        TitleLabel.Text = "Edit Address";
        SaveButton.Text = "Save";
        Line1Entry.Text = existing.Address.AddressLine1;
        Line2Entry.Text = existing.Address.AddressLine2;
        CityEntry.Text = existing.Address.City;
        StateEntry.Text = existing.Address.StateProvince;
        PostalEntry.Text = existing.Address.PostalCode;
        CountryEntry.Text = existing.Address.Country;
        PrimarySwitch.IsToggled = existing.IsPrimary;

        var tagIndex = Array.IndexOf(TagValues, existing.Tag);
        if (tagIndex >= 0) TagPicker.SelectedIndex = tagIndex;
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
        => await CloseAsync(null!);

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Line1Entry.Text) && string.IsNullOrWhiteSpace(CityEntry.Text))
            return;

        var tag = TagPicker.SelectedIndex >= 0 && TagPicker.SelectedIndex < TagValues.Length
            ? TagValues[TagPicker.SelectedIndex] : 0;

        await CloseAsync(new AddAddressResult(
            Line1Entry.Text?.Trim(),
            Line2Entry.Text?.Trim(),
            CityEntry.Text?.Trim(),
            StateEntry.Text?.Trim(),
            PostalEntry.Text?.Trim(),
            CountryEntry.Text?.Trim(),
            tag,
            PrimarySwitch.IsToggled));
    }
}
