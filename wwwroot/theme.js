window.themeManager = {
    setTheme: function (theme) {
        let actualTheme = theme;
        if (theme === 'System' || theme === 2) { // AppTheme.System
            actualTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        } else if (theme === 'Dark' || theme === 1) { // AppTheme.Dark
            actualTheme = 'dark';
        } else {
            actualTheme = 'light';
        }
        
        document.documentElement.setAttribute('data-theme', actualTheme);
        localStorage.setItem('app-theme', theme);
        
        const baseThemeLink = document.getElementById('radzen-base-theme');
        const themeLink = document.getElementById('radzen-theme');
        
        if (actualTheme === 'dark') {
            baseThemeLink.href = '_content/Radzen.Blazor/css/material-dark-base.css';
            themeLink.href = '_content/Radzen.Blazor/css/material-dark.css';
        } else {
            baseThemeLink.href = '_content/Radzen.Blazor/css/material-base.css';
            themeLink.href = '_content/Radzen.Blazor/css/material.css';
        }
        
        // Notify leaflet if it exists
        if (window.updateMapTheme) {
            window.updateMapTheme(actualTheme);
        }
    },
    getStoredTheme: function () {
        const stored = localStorage.getItem('app-theme');
        return stored || 'System';
    },
    init: function () {
        const theme = this.getStoredTheme();
        this.setTheme(theme);
        
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
            const stored = localStorage.getItem('app-theme');
            if (!stored || stored === 'System' || stored === '2') {
                this.setTheme('System');
            }
        });
        
        return theme;
    }
};
