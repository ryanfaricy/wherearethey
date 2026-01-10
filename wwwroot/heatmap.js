let map;
let heatLayer;
let tileLayer;

window.initHeatMap = function (elementId, initialLat, initialLng, reports, dotNetHelper) {
    if (map) {
        map.remove();
    }

    const hasInitialLocation = initialLat !== 0 || initialLng !== 0;
    map = L.map(elementId).setView([initialLat, initialLng], hasInitialLocation ? 13 : 2);

    updateMapTheme();

    map.on('click', function(e) {
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnMapClick', e.latlng.lat, e.latlng.lng);
        }
    });

    map.on('dblclick', function(e) {
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnMapDoubleClick', e.latlng.lat, e.latlng.lng);
        }
    });
    map.doubleClickZoom.disable();

    updateHeatMap(reports, !hasInitialLocation);
};

window.updateMapTheme = function (theme) {
    if (!map) return;

    if (tileLayer) {
        map.removeLayer(tileLayer);
    }

    // Determine theme if not provided
    if (!theme) {
        theme = document.documentElement.getAttribute('data-theme') || 'light';
    }

    const isDark = theme === 'dark';
    const baseUrl = isDark 
        ? 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png'
        : 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png';

    tileLayer = L.tileLayer(baseUrl, {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>',
        subdomains: 'abcd',
        maxZoom: 20
    }).addTo(map);
};

window.updateHeatMap = function (reports, shouldFitBounds = true) {
    if (!map) return;

    if (heatLayer) {
        map.removeLayer(heatLayer);
    }

    // Increased intensity for normal reports (0.5 -> 0.8) and emergency (1.0)
    const heatData = reports.map(r => [r.latitude, r.longitude, r.isEmergency ? 1.0 : 0.8]);
    
    // High-contrast configuration:
    // - Increased radius and reduced blur for sharper hotspots
    // - Higher minOpacity to ensure faint reports are visible
    // - More aggressive gradient starting earlier
    heatLayer = L.heatLayer(heatData, {
        radius: 30,
        blur: 10,
        maxZoom: 17,
        minOpacity: 0.5,
        gradient: {
            0.2: 'blue',
            0.4: 'cyan',
            0.6: 'lime',
            0.8: 'yellow',
            1.0: 'red'
        }
    }).addTo(map);

    if (shouldFitBounds && reports.length > 0) {
        const bounds = L.latLngBounds(reports.map(r => [r.latitude, r.longitude]));
        map.fitBounds(bounds, { padding: [50, 50], maxZoom: 15 });
    }
};
