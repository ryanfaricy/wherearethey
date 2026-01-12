let map;
let heatLayer;
let tileLayer;
let alertLayers = [];

window.initHeatMap = function (elementId, initialLat, initialLng, reports, dotNetHelper, alerts) {
    if (map) {
        map.remove();
        alertLayers = [];
    }

    const hasInitialLocation = initialLat !== 0 || initialLng !== 0;
    map = L.map(elementId).setView([initialLat, initialLng], hasInitialLocation ? 13 : 2);

    updateMapTheme();

    map.on('click', function(e) {
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnMapClick', e.latlng.lat, e.latlng.lng);
        }
    });

    map.on('contextmenu', function(e) {
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnMapContextMenu', e.latlng.lat, e.latlng.lng);
        }
    });
    // Re-enable double click zoom if we are not using dblclick for custom actions
    map.doubleClickZoom.enable();

    updateHeatMap(reports, !hasInitialLocation);
    
    if (alerts) {
        updateAlerts(alerts);
    }
};

window.updateAlerts = function (alerts) {
    if (!map) return;

    // Clear existing alert layers
    alertLayers.forEach(layer => map.removeLayer(layer));
    alertLayers = [];

    if (!alerts || !alerts.length) return;

    alerts.forEach(alert => {
        // Create circle
        const circle = L.circle([alert.latitude, alert.longitude], {
            radius: alert.radiusKm * 1000,
            color: '#ff9800', // Orange-ish
            fillColor: '#ff9800',
            fillOpacity: 0.1,
            weight: 2,
            dashArray: '5, 10',
            interactive: false // Don't block clicks
        }).addTo(map);
        alertLayers.push(circle);

        // Create small pin/marker
        const marker = L.circleMarker([alert.latitude, alert.longitude], {
            radius: 5,
            color: '#e65100',
            fillColor: '#ffb74d',
            fillOpacity: 1,
            weight: 2
        }).addTo(map);

        if (alert.message) {
            marker.bindTooltip(alert.message, {
                permanent: false,
                direction: 'top'
            });
        }
        alertLayers.push(marker);
    });
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

window.getMapState = function() {
    if (!map) return null;
    const center = map.getCenter();
    const bounds = map.getBounds();
    // Calculate radius in km from center to corner (diagonal)
    // This gives a good default coverage for the visible area
    const radiusKm = center.distanceTo(bounds.getNorthEast()) / 1000;
    return {
        lat: center.lat,
        lng: center.lng,
        radiusKm: radiusKm
    };
};

window.setMapView = function (lat, lng, radiusKm) {
    if (!map) return;
    
    // Zoom level 13 is a good default (~5km radius area)
    // If radius is provided, we can try to fit it
    if (radiusKm) {
        // Approximate zoom level based on radius
        // radius 5km -> zoom 13
        // radius 10km -> zoom 12
        // radius 20km -> zoom 11
        // radius 40km -> zoom 10
        const zoom = Math.max(2, Math.min(18, 15 - Math.log2(radiusKm)));
        map.setView([lat, lng], Math.round(zoom));
    } else {
        map.setView([lat, lng], 13);
    }
};
