let deferredPrompt;

window.addEventListener('beforeinstallprompt', (e) => {
    // Prevent the mini-infobar from appearing on mobile
    e.preventDefault();
    // Stash the event so it can be triggered later.
    deferredPrompt = e;
    
    // Notify the UI that the install prompt is available
    if (window.dotNetPwaHelper) {
        window.dotNetPwaHelper.invokeMethodAsync('SetInstallable', true);
    }
});

window.addEventListener('appinstalled', (evt) => {
    deferredPrompt = null;
    if (window.dotNetPwaHelper) {
        window.dotNetPwaHelper.invokeMethodAsync('SetInstallable', false);
    }
    console.log('AreTheyHere was installed');
});

window.pwaFunctions = {
    registerHelper: function (helper) {
        window.dotNetPwaHelper = helper;
        // Check if we already have a prompt waiting
        if (deferredPrompt) {
            window.dotNetPwaHelper.invokeMethodAsync('SetInstallable', true);
        }
    },
    showInstallPrompt: async function () {
        if (!deferredPrompt) {
            return false;
        }
        deferredPrompt.prompt();
        const { outcome } = await deferredPrompt.userChoice;
        deferredPrompt = null;
        return outcome === 'accepted';
    },
    isPwa: function() {
        return window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;
    },
    isIOS: function() {
        return (/iPad|iPhone|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)) && !window.MSStream;
    }
};
