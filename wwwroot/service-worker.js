const CACHE_NAME = 'aretheyhere-cache-v2';
const OFFLINE_URL = 'offline.html';
const ASSETS_TO_CACHE = [
    OFFLINE_URL,
    'favicon.png',
    'manifest.json'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            return cache.addAll(ASSETS_TO_CACHE);
        })
    );
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.filter(name => name !== CACHE_NAME).map(name => caches.delete(name))
            );
        })
    );
});

self.addEventListener('fetch', event => {
    // Only handle http/https requests
    if (!event.request.url.startsWith('http')) {
        return;
    }

    if (event.request.mode === 'navigate') {
        event.respondWith(
            fetch(event.request).catch(error => {
                console.log('[ServiceWorker] Navigation failed; returning offline page.', error);
                return caches.match(OFFLINE_URL);
            })
        );
        return;
    }

    // Basic fetch handler to satisfy PWA requirements and provide some caching
    event.respondWith(
        caches.match(event.request).then(response => {
            return response || fetch(event.request).catch(error => {
                // Catching the error here prevents "Uncaught (in promise) TypeError: Failed to fetch"
                return Response.error();
            });
        })
    );
});

self.addEventListener('push', event => {
    let data = {};
    try {
        data = event.data ? event.data.json() : {};
        console.log('Push received:', data);
    } catch (e) {
        console.error('Push data is not JSON or is invalid:', e);
        try {
            const text = event.data.text();
            data = { message: text };
        } catch (e2) {
            data = { message: 'New alert received' };
        }
    }

    const title = data.title || 'AreTheyHere Alert';
    const options = {
        body: data.message || 'New incident reported in your watched area.',
        icon: 'favicon.png',
        badge: 'favicon.png',
        tag: data.url || 'general-alert', // Use URL as tag to group notifications for same incident
        renotify: true,
        data: {
            url: data.url || '/'
        },
        vibrate: [200, 100, 200],
        actions: [
            { action: 'open', title: 'View Map', icon: 'favicon.png' }
        ]
    };

    event.waitUntil(
        self.registration.showNotification(title, options)
            .catch(err => console.error('Error showing notification:', err))
    );
});

self.addEventListener('notificationclick', event => {
    event.notification.close();
    if (event.notification.data && event.notification.data.url) {
        event.waitUntil(
            clients.openWindow(event.notification.data.url)
        );
    }
});
