import { dotnetRuntime } from './main.js';
const exports = await dotnetRuntime.getAssemblyExports("Avalonia.Controls.Maui.Essentials");

export const accelerometerInterop = {
    frequency: 10,
    lastUpdateTime: 0,
    startListening: function (frequency) {

        this.frequency = frequency;
        if ('DeviceMotionEvent' in window) {

            function onDeviceMotion(event) {
                const now = Date.now();
                const interval = 1000 / this.frequency;

                if (now - this.lastUpdateTime < interval) return;
                this.lastUpdateTime = now;

                const acceleration = event.accelerationIncludingGravity;

                exports.Avalonia.Controls.Maui.Essentials.AvaloniaAccelerometer.OnReadingChanged(
                    acceleration?.x || 0,
                    acceleration?.y || 0,
                    acceleration?.z || 0
                );
            }

            if (typeof DeviceMotionEvent.requestPermission === 'function') {
                DeviceMotionEvent.requestPermission()
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
        if ('DeviceMotionEvent' in window)
            window.ondevicemotion = null;
        this.lastUpdateTime = 0;
    }
};