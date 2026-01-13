let map;
let heatLayer;
let tileLayer;
let alertLayers = [];
let reportMarkers = [];
let alertMarkers = [];
let allReports = [];
let markerClusterGroup;
let userLocationMarker;
let userLocationCircle;
let selectedReportId = null;
let resizeObserver = null;
let dotNetHelper;
const PIN_ZOOM_THRESHOLD = 15;

let translations = {};

window.initHeatMap = function (elementId, initialLat, initialLng, reports, helper, alerts, t) {
    if (t) translations = t;
    if (map) {
        if (resizeObserver) {
            resizeObserver.disconnect();
            resizeObserver = null;
        }
        map.remove();
        alertLayers = [];
        alertMarkers = [];
        reportMarkers = [];
        userLocationMarker = null;
        userLocationCircle = null;
    }

    dotNetHelper = helper;
    const container = document.getElementById(elementId);
    map = L.map(elementId).setView([initialLat, initialLng], (initialLat !== 0 || initialLng !== 0) ? 13 : 2);

    markerClusterGroup = L.markerClusterGroup({
        showCoverageOnHover: false,
        zoomToBoundsOnClick: true,
        spiderfyOnMaxZoom: true,
        maxClusterRadius: 40
    });

    if (container) {
        resizeObserver = new ResizeObserver(() => {
            if (map) {
                map.invalidateSize();
            }
        });
        resizeObserver.observe(container);
    }

    const hasInitialLocation = initialLat !== 0 || initialLng !== 0;

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

    map.on('zoomend', function() {
        updatePinsVisibility();
    });

    // Re-enable double click zoom if we are not using dblclick for custom actions
    map.doubleClickZoom.enable();

    updateHeatMap(reports, !hasInitialLocation);
    
    if (alerts) {
        updateAlerts(alerts);
    }
};

window.updatePinsVisibility = function() {
    if (!map || !markerClusterGroup) return;
    const zoom = map.getZoom();
    if (zoom >= PIN_ZOOM_THRESHOLD) {
        if (!map.hasLayer(markerClusterGroup)) {
            map.addLayer(markerClusterGroup);
        }
    } else {
        if (map.hasLayer(markerClusterGroup)) {
            map.removeLayer(markerClusterGroup);
        }
    }
};

function refreshClusterGroup() {
    if (!markerClusterGroup) return;
    markerClusterGroup.clearLayers();
    reportMarkers.forEach(m => markerClusterGroup.addLayer(m));
    alertMarkers.forEach(m => markerClusterGroup.addLayer(m));
}

window.getZoomLevel = function () {
    return map ? map.getZoom() : 0;
};

function onMarkerClick(e) {
    if (e.originalEvent) {
        L.DomEvent.stopPropagation(e.originalEvent);
    }
    const latlng = e.target.getLatLng();
    if (dotNetHelper) {
        dotNetHelper.invokeMethodAsync('OnMapClick', latlng.lat, latlng.lng, true);
    }
    
    if (e.target.reportId) {
        window.selectReport(e.target.reportId);
    }
}

window.updateAlerts = function (alerts) {
    if (!map) return;

    // Clear existing alert layers
    alertLayers.forEach(layer => map.removeLayer(layer));
    alertLayers = [];
    alertMarkers = [];

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
            radius: 7,
            color: '#e65100',
            fillColor: '#ffb74d',
            fillOpacity: 1,
            weight: 2
        });

        marker.alertData = alert;
        marker.on('click', onMarkerClick);

        marker.bindTooltip(alert.message || translations.Alert_Zone || 'Alert Zone', {
            permanent: false,
            direction: 'top'
        });
        
        alertMarkers.push(marker);
        alertLayers.push(marker);
    });
    
    refreshClusterGroup();
    updatePinsVisibility();
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
    allReports = reports;

    if (heatLayer) {
        map.removeLayer(heatLayer);
    }

    // Clear old markers
    reportMarkers = [];

    // Create markers for reports
    reports.forEach(r => {
        const isSelected = r.id === selectedReportId;
        const color = isSelected ? '#ffeb3b' : (r.isEmergency ? '#f44336' : '#2196f3');
        const marker = L.circleMarker([r.latitude, r.longitude], {
            radius: isSelected ? 8 : 6,
            color: '#fff',
            fillColor: color,
            fillOpacity: 0.9,
            weight: 2
        });

        marker.reportData = r;
        marker.on('click', onMarkerClick);

        marker.reportId = r.id;

        const tooltipText = r.message ? 
            (r.isEmergency ? '🚨 ' + r.message : r.message) : 
            (r.isEmergency ? '🚨 ' + (translations.EMERGENCY_REPORT || 'EMERGENCY') : (translations.Report || 'Report'));

        marker.bindTooltip(tooltipText, {
            permanent: false,
            direction: 'top'
        });

        reportMarkers.push(marker);
    });

    refreshClusterGroup();
    updatePinsVisibility();

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

window.selectReport = function(reportId) {
    selectedReportId = reportId;
    reportMarkers.forEach(m => {
        const report = allReports.find(r => r.id === m.reportId);
        if (report) {
            const isSel = report.id === selectedReportId;
            m.setStyle({
                radius: isSel ? 8 : 6,
                fillColor: isSel ? '#ffeb3b' : (report.isEmergency ? '#f44336' : '#2196f3')
            });
            if (isSel) {
                m.bringToFront();
            }
        }
    });
};

window.focusReport = function(reportId) {
    selectedReportId = reportId;
    const marker = reportMarkers.find(m => m.reportId === reportId);
    if (marker) {
        if (markerClusterGroup && !map.hasLayer(markerClusterGroup)) {
            map.addLayer(markerClusterGroup);
        }
        map.setView(marker.getLatLng(), 17);
        
        // If the marker is in a cluster, we might need to spiderfy it or just zoom more
        // For now, just trigger the click
        onMarkerClick({ target: marker, latlng: marker.getLatLng() });
        window.selectReport(reportId);
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

window.updateUserLocation = function (lat, lng, accuracy) {
    if (!map) return;

    if (userLocationMarker) {
        userLocationMarker.setLatLng([lat, lng]);
    } else {
        userLocationMarker = L.circleMarker([lat, lng], {
            radius: 8,
            fillColor: '#2196F3',
            color: '#fff',
            weight: 2,
            opacity: 1,
            fillOpacity: 1,
            pane: 'markerPane',
            interactive: false
        }).addTo(map);
    }

    if (userLocationCircle) {
        userLocationCircle.setLatLng([lat, lng]);
        userLocationCircle.setRadius(accuracy);
    } else if (accuracy) {
        userLocationCircle = L.circle([lat, lng], {
            radius: accuracy,
            color: '#2196F3',
            fillColor: '#2196F3',
            fillOpacity: 0.15,
            weight: 1,
            interactive: false
        }).addTo(map);
    }
};
