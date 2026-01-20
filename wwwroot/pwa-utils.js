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
    },
    isPushSupported: function() {
        return 'Notification' in window && 'serviceWorker' in navigator && 'PushManager' in window;
    },
    requestPushPermission: async function() {
        if (!('Notification' in window)) {
            return 'unsupported';
        }
        return await Notification.requestPermission();
    },
    getPushSubscription: async function() {
        if (!('serviceWorker' in navigator)) return null;
        const registration = await navigator.serviceWorker.ready;
        if (!registration.pushManager) return null;
        
        const subscription = await registration.pushManager.getSubscription();
        if (!subscription) return null;
        
        return JSON.parse(JSON.stringify(subscription));
    },
    subscribeUser: async function(vapidPublicKey) {
        try {
            if (!('serviceWorker' in navigator)) return null;
            const registration = await navigator.serviceWorker.ready;
            if (!registration.pushManager) return null;

            const subscribeOptions = {
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(vapidPublicKey)
            };

            const subscription = await registration.pushManager.subscribe(subscribeOptions);
            return JSON.parse(JSON.stringify(subscription));
        } catch (error) {
            console.error('Failed to subscribe the user: ', error);
            return null;
        }
    }
};

function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding)
        .replace(/\-/g, '+')
        .replace(/_/g, '/');

    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
}
