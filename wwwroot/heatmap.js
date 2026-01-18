let map;
let heatLayer;
let tileLayer;
let alertLayers = [];
let reportMarkers = [];
let allReports = [];
let markerClusterGroup;
let userLocationMarker;
let userLocationCircle;
let selectedReportId = null;
let resizeObserver = null;
let dotNetHelper;
let isProgrammaticMove = false;
let ghostMarker = null;
const PIN_ZOOM_THRESHOLD = 15;

let translations = {};

let isAdminMode = false;
let isAlertCreationMode = false;
let selectionCircle = null;
let selectionCenterMarker = null;
let dragStartLatLng = null;

window.initHeatMap = function (elementId, initialLat, initialLng, reports, helper, alerts, t, isAdmin) {
    if (t) translations = t;
    isAdminMode = isAdmin || false;
    if (map) {
        if (resizeObserver) {
            resizeObserver.disconnect();
            resizeObserver = null;
        }
        map.remove();
        alertLayers = [];
        reportMarkers = [];
        userLocationMarker = null;
        userLocationCircle = null;
        ghostMarker = null;
        selectionCircle = null;
        selectionCenterMarker = null;
    }

    dotNetHelper = helper;
    const container = document.getElementById(elementId);
    map = L.map(elementId, {
        preferCanvas: true,
    }).setView([initialLat, initialLng], (initialLat !== 0 || initialLng !== 0) ? 13 : 2);

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
            dotNetHelper.invokeMethodAsync('OnMapClick', e.latlng.lat, e.latlng.lng, false, null, null);
        }
    });

    map.on('contextmenu', function(e) {
        if (e.originalEvent) {
            L.DomEvent.preventDefault(e.originalEvent);
        }
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnMapContextMenu', e.latlng.lat, e.latlng.lng);
        }
    });

    map.on('zoomend', function() {
        updatePinsVisibility();
    });

    map.on('moveend', function() {
        if (dotNetHelper) {
            const center = map.getCenter();
            dotNetHelper.invokeMethodAsync('OnMapMoveEnd', center.lat, center.lng);
        }
    });

    // Re-enable double click zoom if we are not using dblclick for custom actions
    map.doubleClickZoom.enable();

    map.on('mousedown', function (e) {
        if (!isAlertCreationMode) return;
        dragStartLatLng = e.latlng;
        map.dragging.disable();

        selectionCenterMarker = L.circleMarker(dragStartLatLng, {
            radius: 6,
            color: '#fff',
            fillColor: '#ff9800',
            fillOpacity: 0.6,
            weight: 2,
            dashArray: '3, 3',
            interactive: false
        }).addTo(map);

        selectionCircle = L.circle(dragStartLatLng, {
            radius: 0,
            color: '#ff9800',
            fillColor: '#ff9800',
            fillOpacity: 0.2,
            weight: 2,
            dashArray: '5, 5'
        }).addTo(map);
    });

    map.on('mousemove', function (e) {
        if (!isAlertCreationMode || !dragStartLatLng || !selectionCircle) return;
        const radius = dragStartLatLng.distanceTo(e.latlng);
        selectionCircle.setRadius(radius);
    });

    map.on('mouseup', function (e) {
        if (!isAlertCreationMode || !dragStartLatLng || !selectionCircle) return;
        const radiusKm = dragStartLatLng.distanceTo(e.latlng) / 1000;
        const center = dragStartLatLng;

        // Cleanup
        if (selectionCircle) {
            map.removeLayer(selectionCircle);
            selectionCircle = null;
        }
        if (selectionCenterMarker) {
            map.removeLayer(selectionCenterMarker);
            selectionCenterMarker = null;
        }
        dragStartLatLng = null;
        map.dragging.enable();

        if (dotNetHelper && radiusKm > 0.01) {
            dotNetHelper.invokeMethodAsync('OnAlertAreaDefined', center.lat, center.lng, radiusKm);
        }
    });

    map.on('movestart', function() {
        if (!isProgrammaticMove && dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnUserInteractedWithMap');
        }
    });

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
}

window.getZoomLevel = function () {
    return map ? map.getZoom() : 0;
};

function onMarkerClick(e) {
    if (e.originalEvent) {
        L.DomEvent.stopPropagation(e.originalEvent);
    }
    const latlng = e.target.getLatLng();
    
    const reportId = e.target.reportId || null;
    const alertId = e.target.alertData ? e.target.alertData.id : null;

    if (dotNetHelper) {
        dotNetHelper.invokeMethodAsync('OnMapClick', latlng.lat, latlng.lng, true, reportId, alertId);
    }
    
    if (reportId) {
        window.selectReport(reportId);
    }
}

window.scrollToElement = function (id) {
    const element = document.getElementById(id);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
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
            color: '#e65100', // Darker orange
            fillColor: '#ff9800',
            fillOpacity: 0.05,
            weight: 1.5,
            interactive: false
        }).addTo(map);
        alertLayers.push(circle);

        // Create marker with icon
        const alertIcon = L.divIcon({
            className: 'alert-pin-container',
            html: '<div class="alert-pin-inner"><i class="rzi">notifications</i></div>',
            iconSize: [44, 44],
            iconAnchor: [22, 22]
        });

        const marker = L.marker([alert.latitude, alert.longitude], {
            icon: alertIcon
        }).addTo(map);

        marker.alertData = alert;
        marker.on('click', onMarkerClick);

        marker.bindTooltip(alert.message || translations.Alert_Zone || 'Alert Zone', {
            permanent: false,
            direction: 'top'
        });
        
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
        addReportMarker(r);
    });

    refreshClusterGroup();
    updatePinsVisibility();
    refreshHeatLayer();

    if (shouldFitBounds && reports.length > 0) {
        const bounds = L.latLngBounds(reports.map(r => [r.latitude, r.longitude]));
        isProgrammaticMove = true;
        map.fitBounds(bounds, { padding: [50, 50], maxZoom: 15 });
        isProgrammaticMove = false;
    }
};

window.addSingleReport = function (report) {
    if (!map) return;
    
    // Check if it already exists (to avoid duplicates)
    if (allReports.some(r => r.id === report.id)) return;
    
    allReports.push(report);
    addReportMarker(report);
    
    refreshClusterGroup();
    updatePinsVisibility();
    refreshHeatLayer();
};

window.removeSingleReport = function (reportId) {
    if (!map) return;

    const markerIndex = reportMarkers.findIndex(m => m.reportId === reportId);
    if (markerIndex !== -1) {
        const marker = reportMarkers[markerIndex];
        if (markerClusterGroup) {
            markerClusterGroup.removeLayer(marker);
        }
        reportMarkers.splice(markerIndex, 1);
    }

    const reportIndex = allReports.findIndex(r => r.id === reportId);
    if (reportIndex !== -1) {
        allReports.splice(reportIndex, 1);
        refreshHeatLayer();
    }
};

function createReportIcon(r, isSelected) {
    const type = r.isEmergency ? 'emergency' : 'normal';
    const selectedClass = isSelected ? 'selected' : '';
    const iconName = r.isEmergency ? 'report_problem' : 'location_on';
    
    return L.divIcon({
        className: `report-pin-container ${type} ${selectedClass}`,
        html: `<div class="report-pin-inner"><i class="rzi">${iconName}</i></div>`,
        iconSize: [44, 44],
        iconAnchor: [22, 22]
    });
}

function addReportMarker(r) {
    const isSelected = r.id === selectedReportId;
    const marker = L.marker([r.latitude, r.longitude], {
        icon: createReportIcon(r, isSelected)
    });

    marker.reportData = r;
    marker.on('click', onMarkerClick);

    marker.reportId = r.id;
    
    if (isSelected) {
        marker.setZIndexOffset(1000);
    }

    let tooltipText = r.message ? 
        (r.isEmergency ? '🚨 ' + r.message : r.message) : 
        (r.isEmergency ? '🚨 ' + (translations.EMERGENCY_REPORT || 'EMERGENCY') : (translations.Report || 'Report'));

    if (isAdminMode) {
        const id = r.reporterIdentifier ? r.reporterIdentifier.substring(0, 8) : '???';
        tooltipText = `<b>${tooltipText}</b><br/><small>Reporter: ${id}</small>`;
    }

    marker.bindTooltip(tooltipText, {
        permanent: false,
        direction: 'top',
        offset: [0, -10],
        html: isAdminMode // Enable HTML for admin tooltips
    });

    if (isAdminMode && r.reporterLatitude && r.reporterLongitude) {
        const reporterMarker = L.circleMarker([r.reporterLatitude, r.reporterLongitude], {
            radius: 4,
            color: r.isEmergency ? '#ff4444' : '#3388ff',
            fillColor: r.isEmergency ? '#ff4444' : '#3388ff',
            fillOpacity: 0.6,
            weight: 1
        }).bindTooltip(`Reporter Location for #${r.id}`);
        
        const line = L.polyline([
            [r.latitude, r.longitude],
            [r.reporterLatitude, r.reporterLongitude]
        ], {
            color: r.isEmergency ? '#ff4444' : '#3388ff',
            weight: 2,
            dashArray: '5, 5',
            opacity: 0.4
        });
        
        marker.reporterLayer = L.layerGroup([reporterMarker, line]);
        
        // Admin optimization: ONLY show reporter info for the SELECTED report
        // This prevents the map from becoming cluttered with hundreds of circles/lines
        const updateReporterLayer = () => {
            if (map && marker.reporterLayer) {
                const isSelected = r.id === selectedReportId;
                const shouldShow = isSelected && map.getZoom() >= PIN_ZOOM_THRESHOLD;
                
                if (shouldShow) {
                    if (!map.hasLayer(marker.reporterLayer)) marker.reporterLayer.addTo(map);
                } else {
                    if (map.hasLayer(marker.reporterLayer)) map.removeLayer(marker.reporterLayer);
                }
            }
        };

        marker.on('add', updateReporterLayer);
        marker.on('remove', () => {
            if (map.hasLayer(marker.reporterLayer)) map.removeLayer(marker.reporterLayer);
        });

        map.on('zoomend', updateReporterLayer);
        
        // We also need to trigger this when selection changes
        // This is handled in window.selectReport
    }

    reportMarkers.push(marker);
}

function refreshHeatLayer() {
    if (!map) return;

    try {
        if (heatLayer) {
            map.removeLayer(heatLayer);
        }

        // Increased intensity for normal reports (0.5 -> 0.8) and emergency (1.0)
        const heatData = allReports.map(r => [r.latitude, r.longitude, r.isEmergency ? 1.0 : 0.8]);

        // High-contrast configuration
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
    } catch (e) {
        console.error('Heatmap error:', e);
    }
}

window.selectReport = function(reportId) {
    selectedReportId = reportId;
    reportMarkers.forEach(m => {
        const report = allReports.find(r => r.id === m.reportId);
        if (report) {
            const isSel = report.id === selectedReportId;
            m.setIcon(createReportIcon(report, isSel));
            if (isSel) {
                m.setZIndexOffset(1000);
            } else {
                m.setZIndexOffset(0);
            }
            
            // If it's admin mode, we also need to update the reporterLayer visibility
            if (isAdminMode && m.reporterLayer && map.hasLayer(m)) {
                const shouldShow = isSel && map.getZoom() >= PIN_ZOOM_THRESHOLD;
                if (shouldShow) {
                    if (!map.hasLayer(m.reporterLayer)) m.reporterLayer.addTo(map);
                } else {
                    if (map.hasLayer(m.reporterLayer)) map.removeLayer(m.reporterLayer);
                }
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
        isProgrammaticMove = true;
        map.setView(marker.getLatLng(), 17);
        isProgrammaticMove = false;
        
        // If the marker is in a cluster, we might need to spiderfy it or just zoom more
        // For now, just trigger the click
        onMarkerClick({ target: marker, latlng: marker.getLatLng() });
        window.selectReport(reportId);
    }
};

window.setAlertCreationMode = function (enabled) {
    isAlertCreationMode = enabled;
    if (!enabled) {
        if (selectionCircle) {
            map.removeLayer(selectionCircle);
            selectionCircle = null;
        }
        if (selectionCenterMarker) {
            map.removeLayer(selectionCenterMarker);
            selectionCenterMarker = null;
        }
        dragStartLatLng = null;
        if (map) map.dragging.enable();
    }
};

window.getMapState = function() {
    if (!map) return null;
    const center = map.getCenter();
    const bounds = map.getBounds();
    
    // Use the distance to the nearest edge (inscribed circle) 
    // rather than the corner (circumscribed circle) to avoid "way too big" radius
    const northEdge = L.latLng(bounds.getNorth(), center.lng);
    const eastEdge = L.latLng(center.lat, bounds.getEast());
    const distVertical = center.distanceTo(northEdge) / 1000;
    const distHorizontal = center.distanceTo(eastEdge) / 1000;
    const radiusKm = Math.min(distVertical, distHorizontal);

    return {
        lat: center.lat,
        lng: center.lng,
        radiusKm: radiusKm
    };
};

window.setMapView = function (lat, lng, radiusKm) {
    if (!map) return;
    
    isProgrammaticMove = true;
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
        const currentZoom = map.getZoom();
        let targetZoom = currentZoom > 0 ? currentZoom : 13;
        
        // If we have no reports and are at a world-view zoom level (<= 2), 
        // zoom in to a sensible default (zoom 8 is roughly 150-250 miles across)
        if (allReports.length === 0 && targetZoom <= 2) {
            targetZoom = 8;
        }
        
        map.setView([lat, lng], targetZoom);
    }
    isProgrammaticMove = false;
};

window.updateUserLocation = function (lat, lng, accuracy) {
    if (!map) return;

    if (userLocationMarker) {
        userLocationMarker.setLatLng([lat, lng]);
    } else {
        const userIcon = L.divIcon({
            className: 'user-location-pulse',
            iconSize: [14, 14],
            iconAnchor: [7, 7]
        });

        userLocationMarker = L.marker([lat, lng], {
            icon: userIcon,
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
            fillOpacity: 0.1,
            weight: 1,
            interactive: false
        }).addTo(map);
    }
};

window.showGhostPin = function (lat, lng) {
    if (!map) return;
    
    if (ghostMarker) {
        ghostMarker.setLatLng([lat, lng]);
    } else {
        ghostMarker = L.circleMarker([lat, lng], {
            radius: 8,
            color: '#fff',
            fillColor: '#f44336',
            fillOpacity: 0.5,
            weight: 2,
            dashArray: '5, 5',
            interactive: false
        }).addTo(map);
    }
};

window.hideGhostPin = function () {
    if (map && ghostMarker) {
        map.removeLayer(ghostMarker);
        ghostMarker = null;
    }
};
