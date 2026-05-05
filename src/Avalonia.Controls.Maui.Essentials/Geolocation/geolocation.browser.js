export const geolocationInterop = {
    isSupported: function () {
        return "geolocation" in navigator;
    },
    getCurrentLocation: async function (highAccuracy, timeout) {
        return new Promise((resolve, reject) => {
            const options = {
                enableHighAccuracy: highAccuracy,
                timeout: timeout,
                maximumAge: 0
            };
            navigator.geolocation.getCurrentPosition(
                (pos) => { resolve(JSON.stringify(pos.coords.toJSON())); },
                (error) => { console.error(error.code, error.message); reject(); },
                options
            );
        });
    },
    startLocationReading: function (successFunc, errFunc, highAccuracy) {
        const options = {
            enableHighAccuracy: highAccuracy,
            timeout: Infinity,
            maximumAge: 0
        };

        return navigator.geolocation.watchPosition((pos) => {
            successFunc(JSON.stringify(pos.coords.toJSON()));
        }, (err) => {
            errFunc(err.code, err.message);
        }, options);
    },
    stopLocationReading: function (watchingId) {
        navigator.geolocation.clearWatch(watchingId);
    }
};