let map;
let heatLayer;

window.initHeatMap = function (elementId, initialLat, initialLng, reports, dotNetHelper) {
    if (map) {
        map.remove();
    }

    const hasInitialLocation = initialLat !== 0 || initialLng !== 0;
    map = L.map(elementId).setView([initialLat, initialLng], hasInitialLocation ? 13 : 2);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    map.on('dblclick', function(e) {
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnMapDoubleClick', e.latlng.lat, e.latlng.lng);
        }
    });
    map.doubleClickZoom.disable();

    updateHeatMap(reports, !hasInitialLocation);
};

window.updateHeatMap = function (reports, shouldFitBounds = true) {
    if (!map) return;

    if (heatLayer) {
        map.removeLayer(heatLayer);
    }

    const heatData = reports.map(r => [r.latitude, r.longitude, r.isEmergency ? 1.0 : 0.5]);
    heatLayer = L.heatLayer(heatData, {
        radius: 25,
        blur: 15,
        maxZoom: 17,
        gradient: { 0.4: 'blue', 0.65: 'lime', 1: 'red' }
    }).addTo(map);

    if (shouldFitBounds && reports.length > 0) {
        const bounds = L.latLngBounds(reports.map(r => [r.latitude, r.longitude]));
        map.fitBounds(bounds, { padding: [50, 50], maxZoom: 15 });
    }
};
