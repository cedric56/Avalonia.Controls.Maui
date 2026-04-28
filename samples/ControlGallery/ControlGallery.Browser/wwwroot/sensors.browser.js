import { dotnetRuntime } from './main.js';
const exports = await dotnetRuntime.getAssemblyExports("Avalonia.Controls.Maui.Essentials");

export const compassInterop = {
    frequency: 5, // Hz (updates per second)
    lastUpdateTime: 0,
    startListening: function (frequency) {
        this.frequency = frequency || 5; // Default to 5 Hz if not provided
        if (typeof window.DeviceOrientationEvent !== 'undefined' &&
            typeof window.DeviceOrientationEvent.requestPermission === 'function') {
            // iOS 13+ requires permission
            window.DeviceOrientationEvent.requestPermission()
                .then(permissionState => {
                    if (permissionState === 'granted') {
                        window.addEventListener('deviceorientation', this.handle);
                    } else {
                        console.warn('Permission to access device orientation denied.');
                    }
                })
                .catch(console.error);
        } else {
            // Non iOS devices
            window.addEventListener('deviceorientationabsolute', this.handle);
        }
    },
    stopListening: function () {
        window.removeEventListener('deviceorientation', this.handle);
        window.removeEventListener('deviceorientationabsolute', this.handle);
        this.lastUpdateTime = 0;
    },
    handle: function (event) {

        if (event.alpha === null) return; // no data

        const now = Date.now();
        const interval = 1000 / this.frequency;

        if (now - this.lastUpdateTime < interval) return;
        this.lastUpdateTime = now;

        // Normalize alpha to 0-360 degrees
        const heading = event.webkitCompassHeading || Math.abs(event.alpha - 360);

        exports.Avalonia.Controls.Maui.Essentials.AvaloniaCompass.OnReadingChanged(
            heading
        );
    }
};

export const accelerometerInterop = {
    frequency: 10,
    lastUpdateTime: 0,
    startListening: function (frequency) {

        this.frequency = frequency;
        if ('DeviceOrientationEvent' in window) {

            function onDeviceMotion(event) {
                const now = Date.now();
                const interval = 1000 / this.frequency;

                if (now - this.lastUpdateTime < interval) return;
                this.lastUpdateTime = now;

                exports.Avalonia.Controls.Maui.Essentials.AvaloniaAccelerometer.OnReadingChanged(
                    event.accelerationIncludingGravity.x || 0,
                    event.accelerationIncludingGravity.y || 0,
                    event.accelerationIncludingGravity.z || 0
                );
            }

            if (typeof DeviceOrientationEvent.requestPermission === 'function') {
                DeviceOrientationEvent.requestPermission()
                    .then(permissionState => {
                        if (permissionState === 'granted') {
                            window.ondevicemotion = onDeviceMotion;
                        }
                    })
                    .catch(console.error);
            }
            else {
                window.ondevicemotion = onDeviceMotion;
            }
        }
    },
    stopListening: function () {
        if ('DeviceOrientationEvent' in window)
            window.ondevicemotion = null;
        this.lastUpdateTime = 0;
    }
};

export const magnetometerInterop = {
    magSensor: null,
    startListening: function (frequency) {
        if ('magnetometer' in navigator) {
            this.magSensor = new Magnetometer({ frequency: frequency });
            this.magSensor.addEventListener("reading", this.handle);
            this.magSensor.start();
        }
    },
    stopListening: function () {
        if (this.magSensor !== null) {
            this.magSensor.removeEventListener("reading", this.handle);
            this.magSensor.stop();
        }
    },
    handle: function (e) {
        exports.Avalonia.Controls.Maui.Essentials.AvaloniaMagnetometer.OnReadingChanged(
            this.magSensor.x || 0,
            this.magSensor.y || 0,
            this.magSensor.z || 0
        );
    }
};

export const gyroscopeInterop = {
    frequency: 15,
    gyroscope: null,
    startListening: function (frequency) {

        this.frequency = frequency;
        if ('Gyroscope' in navigator) {
            this.gyroscope = new Gyroscope({ frequency: frequency });
            this.gyroscope.addEventListener("reading", this.handle);
            this.gyroscope.start();
        }
        else {
            if ('DeviceOrientationEvent' in window) {

                function onDeviceMotion(event) {
                    const now = Date.now();
                    const interval = 1000 / this.frequency;

                    if (now - this.lastUpdateTime < interval) return;
                    this.lastUpdateTime = now;

                    exports.Avalonia.Controls.Maui.Essentials.AvaloniaGyroscope.OnReadingChanged(
                        event.rotationRate.alpha || 0,
                        event.rotationRate.beta || 0,
                        event.rotationRate.gamma || 0
                    );
                }

                if (typeof DeviceOrientationEvent.requestPermission === 'function') {
                    DeviceOrientationEvent.requestPermission()
                        .then(permissionState => {
                            if (permissionState === 'granted') {
                                window.ondevicemotion = onDeviceMotion;
                            }
                        })
                        .catch(console.error);
                }
                else {
                    window.ondevicemotion = onDeviceMotion;
                }
            }
        }
    },
    stopListening: function () {
        if (this.gyroscope !== null) {
            this.gyroscope.removeEventListener("reading", this.handle);
            this.gyroscope.stop();
        }
        else if ('DeviceOrientationEvent' in window) {
            window.ondevicemotion = null;
            this.lastUpdateTime = 0;
        }
    },
    handle: function (e) {
        exports.Avalonia.Controls.Maui.Essentials.AvaloniaGyroscope.OnReadingChanged(
            this.gyroscope.x || 0,
            this.gyroscope.y || 0,
            this.gyroscope.z || 0
        );
    }
};

export const orientationInterop = {
    lastUpdateTime: 0,
    sensor: null,
    startListening: function (frequency) {
        this.frequency = frequency;
        if ('AbsoluteOrientationSensor' in window) {
            this.sensor = new AbsoluteOrientationSensor({ frequency: frequency });
            this.sensor.addEventListener('reading', this.handle);
            this.sensor.start();
        }
        else if ('DeviceOrientationEvent' in window) {

            function onDeviceOrientation(event) {
                const now = Date.now();
                const interval = 1000 / this.frequency;

                if (now - this.lastUpdateTime < interval) return;
                this.lastUpdateTime = now;

                exports.Avalonia.Controls.Maui.Essentials.AvaloniaOrientationSensor.OnReadingChanged(
                    event.alpha || 0,
                    event.beta || 0,
                    event.gamma || 0,
                    0
                );
            }

            if (typeof DeviceOrientationEvent.requestPermission === 'function') {
                DeviceOrientationEvent.requestPermission()
                    .then(permissionState => {
                        if (permissionState === 'granted') {
                            window.ondeviceorientation = onDeviceOrientation;
                        }
                    })
                    .catch(console.error);
            }
            else {
                window.ondeviceorientation = onDeviceOrientation;
            }
        }
    },
    stopListening: function () {
        if (this.sensor)
            this.sensor.removeEventListener("reading", this.handle);
        else if ('DeviceOrientationEvent' in window) {
            window.ondeviceorientation = null;
            this.lastUpdateTime = 0;
        }
    },
    handle: function (event) {
        exports.Avalonia.Controls.Maui.Essentials.AvaloniaOrientationSensor.OnReadingChanged(
            sensor.quaternion[0] || 0,
            sensor.quaternion[1] || 0,
            sensor.quaternion[2] || 0,
            sensor.quaternion[3]
        );
    }
};