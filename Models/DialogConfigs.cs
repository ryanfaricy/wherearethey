using Radzen;

namespace WhereAreThey.Models
{
    public static class DialogConfigs
    {
        /// <summary>
        /// Standard 500px width dialog configuration, non-draggable and non-resizable.
        /// </summary>
        public static DialogOptions Default => new DialogOptions
        {
            Width = "500px",
            CloseDialogOnOverlayClick = true,
            Draggable = false,
            Resizable = false
        };
    }
}
