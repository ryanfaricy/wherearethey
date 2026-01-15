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
            const darkBase = '_content/Radzen.Blazor/css/material-dark-base.css';
            const darkTheme = '_content/Radzen.Blazor/css/material-dark.css';
            if (baseThemeLink && !baseThemeLink.getAttribute('href').includes(darkBase)) {
                baseThemeLink.href = darkBase;
            }
            if (themeLink && !themeLink.getAttribute('href').includes(darkTheme)) {
                themeLink.href = darkTheme;
            }
        } else {
            const lightBase = '_content/Radzen.Blazor/css/material-base.css';
            const lightTheme = '_content/Radzen.Blazor/css/material.css';
            if (baseThemeLink && !baseThemeLink.getAttribute('href').includes(lightBase)) {
                baseThemeLink.href = lightBase;
            }
            if (themeLink && !themeLink.getAttribute('href').includes(lightTheme)) {
                themeLink.href = lightTheme;
            }
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
        
        // Remove immediate style now that Radzen themes are being loaded
        // We delay this slightly to ensure the browser has time to parse the new Radzen CSS
        setTimeout(() => {
            const immediateStyle = document.getElementById('theme-immediate-style');
            if (immediateStyle) {
                immediateStyle.remove();
            }
        }, 500);
        
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
            const stored = localStorage.getItem('app-theme');
            if (!stored || stored === 'System' || stored === '2') {
                this.setTheme('System');
            }
        });
        
        return theme;
    }
};
