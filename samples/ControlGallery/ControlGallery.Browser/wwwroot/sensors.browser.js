// import { dotnetRuntime } from './main.js';
// const exports = await dotnetRuntime.getAssemblyExports("Avalonia.Controls.Maui.Essentials");

export const accelerometerInterop = {
    frequency: 10,
    lastUpdateTime: 0,
    deviceMotionHandler: null,
    startListening: function (frequency, onReadingChanged) {

        this.frequency = frequency;
        if ('DeviceMotionEvent' in window) {

            if (!this.deviceMotionHandler) {
                this.deviceMotionHandler = (event) => {
                    const now = Date.now();
                    const interval = 1000 / this.frequency;

                    if (now - this.lastUpdateTime < interval) return;
                    this.lastUpdateTime = now;

                    const accelerationIncludingGravity = event.accelerationIncludingGravity;

                    onReadingChanged(
                        accelerationIncludingGravity?.x || 0,
                        accelerationIncludingGravity?.y || 0,
                        accelerationIncludingGravity?.z || 0
                    );
                };
            }

            if (typeof DeviceMotionEvent.requestPermission === 'function') {
                DeviceMotionEvent.requestPermission()
                    .then(permissionState => {
                        if (permissionState === 'granted') {
                            window.addEventListener('devicemotion', this.deviceMotionHandler);
                        }
                    })
                    .catch(console.error);
            }
            else {
                window.addEventListener('devicemotion', this.deviceMotionHandler);
            }
        }
    },
    stopListening: function () {
        if (this.deviceMotionHandler) {
            window.removeEventListener('devicemotion', this.deviceMotionHandler);
            this.deviceMotionHandler = null;
        }
        this.lastUpdateTime = 0;
    }
};