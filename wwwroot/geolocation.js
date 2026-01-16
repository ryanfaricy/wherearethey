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
    console.log('Attempting to copy to clipboard');
    if (text) {
        const isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent) && !window.MSStream;

        const fallbackCopy = (textToCopy) => {
            console.log('Using fallback copy');
            const textArea = document.createElement("textarea");
            textArea.value = textToCopy;

            // Minimal styling to avoid layout shift but keep it "visible" for the browser
            textArea.style.position = "fixed";
            textArea.style.top = "0";
            textArea.style.left = "0";
            textArea.style.width = "2em";
            textArea.style.height = "2em";
            textArea.style.padding = "0";
            textArea.style.border = "none";
            textArea.style.outline = "none";
            textArea.style.boxShadow = "none";
            textArea.style.background = "transparent";
            textArea.style.opacity = "0.01";

            document.body.appendChild(textArea);

            // iOS specific selection
            if (isIOS) {
                textArea.contentEditable = true;
                textArea.readOnly = false;
                const range = document.createRange();
                range.selectNodeContents(textArea);
                const selection = window.getSelection();
                selection.removeAllRanges();
                selection.addRange(range);
                textArea.setSelectionRange(0, 999999);
            } else {
                textArea.select();
            }

            try {
                const successful = document.execCommand('copy');
                console.log('Fallback copy ' + (successful ? 'successful' : 'unsuccessful'));
                if (successful && dotNetSettingsHelper) {
                    dotNetSettingsHelper.invokeMethodAsync('NotifyCopied');
                }
            } catch (err) {
                console.error('Fallback copy failed', err);
            }

            document.body.removeChild(textArea);
        };

        if (navigator.clipboard && !isIOS) {
            navigator.clipboard.writeText(text).then(() => {
                console.log('Clipboard copy successful');
                if (dotNetSettingsHelper) dotNetSettingsHelper.invokeMethodAsync('NotifyCopied');
            }).catch(err => {
                console.error('Navigator.clipboard failed, falling back', err);
                fallbackCopy(text);
            });
        } else {
            fallbackCopy(text);
        }
    }
};

window.copyUserIdentifier = function () {
    const id = localStorage.getItem('user-identifier');
    window.copyToClipboard(id);
};
