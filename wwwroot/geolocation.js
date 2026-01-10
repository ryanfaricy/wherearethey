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
    }
    return id;
};
