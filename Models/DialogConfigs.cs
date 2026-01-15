using Radzen;

namespace WhereAreThey.Models
{
    public static class DialogConfigs
    {
        /// <summary>
        /// Standard 500px width dialog configuration, non-draggable, non-resizable.
        /// </summary>
        public static DialogOptions Default => new DialogOptions
        {
            AutoFocusFirstElement = false,
            CloseDialogOnOverlayClick = false,
            Draggable = false,
            Resizable = false,
            ShowClose = true,
            ShowTitle = true,
            Width = "500px",
        };
    }
}
