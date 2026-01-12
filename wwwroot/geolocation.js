window.getLocation = function () {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject(new Error('Geolocation is not supported by your browser'));
        } else {
            navigator.geolocation.getCurrentPosition(
                position => resolve({
                    coords: {
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude
                    }
                }),
                error => reject(new Error(error.message))
            );
        }
    });
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
