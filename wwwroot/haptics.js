window.haptics = {
    vibrate: function (pattern) {
        if (navigator.vibrate) {
            try {
                navigator.vibrate(pattern);
            } catch (e) {
                console.warn('Haptic feedback failed:', e);
            }
        }
    },
    vibrateSuccess: function () {
        this.vibrate(50);
    },
    vibrateError: function () {
        this.vibrate([100, 50, 100]);
    },
    vibrateWarning: function () {
        this.vibrate([70, 40, 70]);
    },
    vibrateEmergency: function () {
        this.vibrate([200, 100, 200, 100, 200]);
    },
    vibrateUpdate: function () {
        this.vibrate([30, 30, 30]);
    }
};
