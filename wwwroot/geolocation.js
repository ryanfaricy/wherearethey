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

let locationDotNetHelper;
window.watchLocation = function (dotNetHelper) {
    if (!navigator.geolocation) {
        return -1;
    }

    locationDotNetHelper = dotNetHelper;

    // Attempt to start orientation tracking as well
    startOrientationTracking(dotNetHelper);

    return navigator.geolocation.watchPosition(
        position => {
            dotNetHelper.invokeMethodAsync('OnLocationUpdated', {
                coords: {
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude,
                    accuracy: position.coords.accuracy,
                    heading: position.coords.heading
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

let lastHeading = null;
let isOrientationListening = false;
function startOrientationTracking(dotNetHelper) {
    if (isOrientationListening) return;

    const handleOrientation = (event) => {
        let heading = null;
        
        // iOS Safari
        if (event.webkitCompassHeading !== undefined) {
            heading = event.webkitCompassHeading;
        } 
        // Standard absolute orientation
        else if (event.absolute === true && event.alpha !== null) {
            // alpha is 0 when pointing north, and increases counter-clockwise.
            // We want degrees clockwise from North.
            heading = (360 - event.alpha) % 360;
        }

        if (heading !== null && Math.abs((lastHeading || 0) - heading) > 1) {
            lastHeading = heading;
            dotNetHelper.invokeMethodAsync('OnOrientationUpdated', heading);
        }
    };

    if (typeof DeviceOrientationEvent !== 'undefined' && typeof DeviceOrientationEvent.requestPermission === 'function') {
        // iOS 13+ requires permission. 
        // We can't request it here because it's not a user gesture.
        console.log('DeviceOrientation permission required for iOS');
    } else {
        window.addEventListener('deviceorientationabsolute', handleOrientation, true);
        window.addEventListener('deviceorientation', handleOrientation, true);
        isOrientationListening = true;
    }
}

window.requestOrientationPermission = function () {
    if (typeof DeviceOrientationEvent !== 'undefined' && typeof DeviceOrientationEvent.requestPermission === 'function') {
        return DeviceOrientationEvent.requestPermission()
            .then(permissionState => {
                if (permissionState === 'granted') {
                    if (locationDotNetHelper) {
                        startOrientationTracking(locationDotNetHelper);
                    }
                    return true;
                }
                return false;
            })
            .catch(err => {
                console.error('Orientation permission error:', err);
                return false;
            });
    }
    return Promise.resolve(true);
};

window.stopWatching = function (watchId) {
    if (watchId !== undefined && watchId !== null && watchId !== -1) {
        navigator.geolocation.clearWatch(watchId);
    }
};

window.getUserIdentifier = function () {
    let id = localStorage.getItem('user-identifier');
    return id;
};

window.generatePassphrase = function () {
    const adjectives = ["swift", "brave", "bright", "calm", "cool", "eager", "fancy", "grand", "happy", "jolly", "kind", "lucky", "noble", "proud", "quick", "rare", "sharp", "smart", "vast", "wise", "bold", "crisp", "fast", "green", "light", "pure", "safe", "warm", "wild", "young", "fierce", "gentle", "silent", "ancient", "modern", "vibrant", "mighty", "humble", "stellar", "cosmic", "icy", "fiery", "golden", "silver", "azure", "crimson", "hidden", "secret", "mystic", "loyal", "shiny", "glossy", "rough", "smooth", "narrow", "broad", "tall", "short", "deep", "dark", "stable", "vivid", "pious"];
    const nouns = ["river", "mountain", "forest", "ocean", "valley", "desert", "island", "garden", "meadow", "harbor", "eagle", "dolphin", "tiger", "falcon", "wolf", "deer", "panda", "koala", "otter", "seal", "star", "moon", "sun", "cloud", "rain", "wind", "snow", "leaf", "tree", "flower", "stream", "peak", "woods", "tide", "canyon", "dune", "reef", "orchard", "field", "bay", "hawk", "whale", "lion", "owl", "bear", "elk", "lynx", "fox", "badger", "comet", "planet", "nebula", "storm", "mist", "gale", "frost", "root", "branch", "bloom", "stone", "rock", "path", "trail", "bridge", "gate", "tower", "shield", "spirit"];
    
    const adj1 = adjectives[Math.floor(Math.random() * adjectives.length)];
    const adj2 = adjectives[Math.floor(Math.random() * adjectives.length)];
    const noun = nouns[Math.floor(Math.random() * nouns.length)];
    const num = Math.floor(Math.random() * 90) + 10;
    
    return `${adj1}-${adj2}-${noun}-${num}`;
};

window.setUserIdentifier = function (id) {
    if (id && id.length > 0) {
        localStorage.setItem('user-identifier', id);
        return true;
    }
    return false;
};

window.isNewUser = function () {
    return localStorage.getItem('user-identifier-new') === 'true';
};

window.clearNewUserFlag = function () {
    localStorage.removeItem('user-identifier-new');
};

let dotNetSettingsHelper;

window.registerSettingsHelper = function (helper) {
    dotNetSettingsHelper = helper;
};

window.copyToClipboard = function (text) {
    console.log('Attempting to copy:', text);
    if (text) {
        const performCopy = (textToCopy) => {
            if (!navigator.clipboard) {
                console.log('Navigator.clipboard not available, using fallback');
                const textArea = document.createElement("textarea");
                textArea.value = textToCopy;
                document.body.appendChild(textArea);
                textArea.select();
                try {
                    document.execCommand('copy');
                    console.log('Fallback copy successful');
                    if (dotNetSettingsHelper) dotNetSettingsHelper.invokeMethodAsync('NotifyCopied');
                } catch (err) {
                    console.error('Fallback copy failed', err);
                }
                document.body.removeChild(textArea);
                return;
            }
            navigator.clipboard.writeText(textToCopy).then(() => {
                console.log('Clipboard copy successful');
                if (dotNetSettingsHelper) dotNetSettingsHelper.invokeMethodAsync('NotifyCopied');
            }).catch(err => {
                console.error('Failed to copy:', err);
                const textArea = document.createElement("textarea");
                textArea.value = textToCopy;
                document.body.appendChild(textArea);
                textArea.select();
                try {
                    document.execCommand('copy');
                    console.log('Fallback copy successful after clipboard failure');
                    if (dotNetSettingsHelper) dotNetSettingsHelper.invokeMethodAsync('NotifyCopied');
                } catch (fallbackErr) {
                    console.error('Fallback copy failed', fallbackErr);
                }
                document.body.removeChild(textArea);
            });
        };

        performCopy(text);
    } else {
        console.warn('No text provided to copy');
    }
};

window.copyUserIdentifier = function () {
    const id = localStorage.getItem('user-identifier');
    window.copyToClipboard(id);
};
