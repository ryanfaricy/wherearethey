using Radzen;

namespace WhereAreThey.Models;

public static class DialogConfigs
{
    /// <summary>
    /// Standard 500px width dialog configuration, non-draggable, non-resizable.
    /// </summary>
    public static DialogOptions Default => new()
    {
        AutoFocusFirstElement = false,
        CloseDialogOnOverlayClick = false,
        Draggable = false,
        Resizable = false,
        ShowClose = true,
        ShowTitle = true,
        Width = "500px",
    };

    /// <summary>
    /// Default dialog configuration with no close button, no escape key, or overlay-close click.
    /// </summary>
    public static DialogOptions NonCloseable
    {
        get
        {
            var options = Default;
            options.ShowClose = false;
            options.CloseDialogOnEsc = false;
            options.CloseDialogOnOverlayClick = false;

            return options;
        }
    }

    /// <summary>
    /// Admin dialog configuration with draggable and resizable.
    /// </summary>
    public static DialogOptions Admin
    {
        get
        {
            var options = Default;
            options.Resizable = true;
            options.Draggable = true;
                
            return options;
        }
    }
}
