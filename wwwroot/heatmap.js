let map;
let heatLayer;

window.initHeatMap = function (elementId, initialLat, initialLng, reports) {
    if (map) {
        map.remove();
    }

    map = L.map(elementId).setView([initialLat, initialLng], 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    updateHeatMap(reports);
};

window.updateHeatMap = function (reports) {
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

    if (reports.length > 0) {
        const bounds = L.latLngBounds(reports.map(r => [r.latitude, r.longitude]));
        map.fitBounds(bounds, { padding: [50, 50], maxZoom: 15 });
    }
};
