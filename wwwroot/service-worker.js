self.addEventListener('install', event => {
    self.skipWaiting();
});

self.addEventListener('activate', event => {
});

self.addEventListener('fetch', event => {
    // Basic fetch handler to satisfy PWA requirements
    event.respondWith(fetch(event.request));
});
