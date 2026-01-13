window.getLocation = function () {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject(new Error('Geolocation is not supported by your browser'));
        } else {
            navigator.geolocation.getCurrentPosition(
                position => resolve({
                    coords: {
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy
                    }
                }),
                error => reject(new Error(error.message)),
                {
                    enableHighAccuracy: true,
                    timeout: 10000,
                    maximumAge: 30000
                }
            );
        }
    });
};

window.watchLocation = function (dotNetHelper) {
    if (!navigator.geolocation) {
        return -1;
    }
    return navigator.geolocation.watchPosition(
        position => {
            dotNetHelper.invokeMethodAsync('OnLocationUpdated', {
                coords: {
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude,
                    accuracy: position.coords.accuracy
                }
            });
        },
        error => {
            console.warn('Geolocation watch error:', error);
        },
        {
            enableHighAccuracy: true,
            maximumAge: 10000,
            timeout: 10000
        }
    );
};

window.stopWatching = function (watchId) {
    if (watchId !== undefined && watchId !== null && watchId !== -1) {
        navigator.geolocation.clearWatch(watchId);
    }
};

window.getUserIdentifier = function () {
    let id = localStorage.getItem('user-identifier');
    if (!id) {
        id = crypto.randomUUID();
        localStorage.setItem('user-identifier', id);
        localStorage.setItem('user-identifier-new', 'true');
    }
    return id;
};

window.setUserIdentifier = function (id) {
    if (id && id.length > 0) {
        localStorage.setItem('user-identifier', id);
        return true;
    }
    return false;
};

window.isNewUser = function () {
    const isNew = localStorage.getItem('user-identifier-new') === 'true';
    if (isNew) {
        localStorage.removeItem('user-identifier-new');
    }
    return isNew;
};
