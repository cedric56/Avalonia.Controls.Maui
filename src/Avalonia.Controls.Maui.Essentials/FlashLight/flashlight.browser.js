export const flashInterop = {
    currentTrack: null,
    isTorchOn: false,

    // Accurate detection - requires actually testing torch support
    isSupported: async function () {
        // Quick API check first
        if (!navigator.mediaDevices || typeof navigator.mediaDevices.getUserMedia !== 'function') {
            return false;
        }

        // Must attempt to access camera to check torch support
        const stream = await navigator.mediaDevices.getUserMedia({
            video: { facingMode: 'environment' }
        });

        const videoTrack = stream.getVideoTracks()[0];
        if (!videoTrack) {
            stream.getTracks().forEach(track => track.stop());
            return false;
        }

        // Check if torch constraint is supported
        const capabilities = videoTrack.getCapabilities();
        const hasTorch = capabilities && capabilities.torch === true;

        // Clean up
        stream.getTracks().forEach(track => track.stop());

        return hasTorch;
    },

    // Activate/deactivate flashlight (must be called after isSupported check)
    activate: async function (activate) {
        if (!await this.isSupported()) {
            return;
        }

        // If we already have a track and state matches, do nothing
        if (this.currentTrack && this.isTorchOn === activate) {
            return;
        }

        // If we need to turn off, just apply constraint
        if (this.currentTrack && !activate) {
            await this.currentTrack.applyConstraints({
                advanced: [{ torch: false }]
            });
            this.isTorchOn = false;
            return;
        }

        // Need to turn on - get fresh stream if no track exists
        if (!this.currentTrack) {
            const stream = await navigator.mediaDevices.getUserMedia({
                video: {
                    facingMode: 'environment'
                }
            });

            this.currentTrack = stream.getVideoTracks()[0];

            // Add ended handler to clean up if something else stops the track
            this.currentTrack.onended = () => {
                this.currentTrack = null;
                this.isTorchOn = false;
            };
        }

        // Apply torch constraint
        await this.currentTrack.applyConstraints({
            advanced: [{ torch: activate }]
        });

        this.isTorchOn = activate;
    },
    // Clean up resources
    release: function () {
        if (this.currentTrack) {
            this.currentTrack.stop();
            this.currentTrack = null;
            this.isTorchOn = false;
        }
    }
};