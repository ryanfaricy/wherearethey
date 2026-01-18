using Microsoft.AspNetCore.Components;
using Radzen;

namespace WhereAreThey.Components.Admin;

/// <summary>
/// Base class for components that need shared responsive layout properties.
/// </summary>
public class LayoutComponentBase : ComponentBase
{
    /// <summary>
    /// Gets or sets whether the component is being rendered for a mobile device.
    /// </summary>
    [Parameter] public bool IsMobile { get; set; }

    /// <summary>
    /// Gets the stack orientation based on whether the device is mobile.
    /// </summary>
    protected Orientation StackOrientation => IsMobile ? Orientation.Vertical : Orientation.Horizontal;

    /// <summary>
    /// Gets the stack alignment based on whether the device is mobile.
    /// </summary>
    protected AlignItems StackAlign => IsMobile ? AlignItems.Stretch : AlignItems.Center;
}
