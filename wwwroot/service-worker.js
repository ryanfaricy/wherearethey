self.addEventListener('install', event => {
    self.skipWaiting();
});

self.addEventListener('activate', event => {
});

self.addEventListener('fetch', event => {
    // Only handle http/https requests to avoid issues with other schemes like chrome-extension://
    if (!event.request.url.startsWith('http')) {
        return;
    }

    // Basic fetch handler to satisfy PWA requirements
    event.respondWith(
        fetch(event.request).catch(error => {
            // Catching the error here prevents "Uncaught (in promise) TypeError: Failed to fetch"
            // from cluttering the console when requests are cancelled or network is down.
            // Returning Response.error() tells the browser it was a network failure.
            return Response.error();
        })
    );
});
