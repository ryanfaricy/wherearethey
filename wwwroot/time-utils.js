window.timeUtils = {
    getTimeZone: function () {
        return Intl.DateTimeFormat().resolvedOptions().timeZone;
    }
};
