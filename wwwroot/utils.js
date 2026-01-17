/**
 * Global utilities for the application
 */

// Select all text in RadzenAutoComplete on focus
document.addEventListener('focusin', function (e) {
    if (e.target && e.target.tagName === 'INPUT') {
        // Check if the input is part of a RadzenAutoComplete
        // RadzenAutoComplete usually has a wrapper with class rz-autocomplete
        // or the input itself might have it depending on version/config
        if (e.target.closest('.rz-autocomplete')) {
            setTimeout(() => {
                if (e.target === document.activeElement) {
                    e.target.select();
                }
            }, 50);
        }
    }
});
