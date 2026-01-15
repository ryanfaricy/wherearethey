window.getLocation = function () {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject(new Error('Geolocation is not supported by your browser'));
        } else {
            const options = {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 30000
            };

            const success = position => resolve({
                coords: {
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude,
                    accuracy: position.coords.accuracy
                }
            });

            const failure = error => {
                if (error.code === error.TIMEOUT && options.enableHighAccuracy) {
                    console.warn('Geolocation timeout with high accuracy, retrying with low accuracy...');
                    options.enableHighAccuracy = false;
                    // Give it another 10 seconds for low accuracy
                    navigator.geolocation.getCurrentPosition(success, err => reject(new Error(err.message)), options);
                } else {
                    reject(new Error(error.message));
                }
            };

            navigator.geolocation.getCurrentPosition(success, failure, options);
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

window.copyUserIdentifier = function () {
    const id = localStorage.getItem('user-identifier');
    if (id) {
        if (!navigator.clipboard) {
            const textArea = document.createElement("textarea");
            textArea.value = id;
            document.body.appendChild(textArea);
            textArea.select();
            try {
                document.execCommand('copy');
            } catch (err) {
                console.error('Fallback copy failed', err);
            }
            document.body.removeChild(textArea);
            return;
        }
        navigator.clipboard.writeText(id).catch(err => {
            console.error('Failed to copy ID: ', err);
            const textArea = document.createElement("textarea");
            textArea.value = id;
            document.body.appendChild(textArea);
            textArea.select();
            try {
                document.execCommand('copy');
            } catch (fallbackErr) {
                console.error('Fallback copy failed', fallbackErr);
            }
            document.body.removeChild(textArea);
        });
    }
};
