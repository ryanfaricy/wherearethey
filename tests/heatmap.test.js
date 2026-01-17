import { describe, it, expect, beforeEach, vi } from 'vitest';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Load the script content
const heatmapJs = fs.readFileSync(path.resolve(__dirname, '../wwwroot/heatmap.js'), 'utf8');

describe('heatmap.js', () => {
    beforeEach(() => {
        // Mock window and global objects needed by heatmap.js
        global.window = global;
        global.document = {
            getElementById: vi.fn(() => ({})),
            createElement: vi.fn(() => ({ style: {} })),
            body: { appendChild: vi.fn() },
            documentElement: { getAttribute: vi.fn(() => 'light') }
        };
        global.L = {
            map: vi.fn(() => ({
                setView: vi.fn().mockReturnThis(),
                on: vi.fn(),
                doubleClickZoom: { enable: vi.fn() },
                getCenter: vi.fn(() => ({ lat: 0, lng: 0 })),
                getBounds: vi.fn(() => ({ 
                    getNorth: () => 0, 
                    getEast: () => 0,
                    getSouth: () => 0,
                    getWest: () => 0
                })),
                getZoom: vi.fn(() => 13),
                hasLayer: vi.fn(() => false),
                addLayer: vi.fn(),
                removeLayer: vi.fn(),
                invalidateSize: vi.fn(),
                remove: vi.fn()
            })),
            markerClusterGroup: vi.fn(() => ({
                addLayer: vi.fn(),
                clearLayers: vi.fn()
            })),
            latLng: vi.fn((lat, lng) => ({ 
                lat, 
                lng,
                distanceTo: vi.fn(() => 2000)
            })),
            DomEvent: { preventDefault: vi.fn() },
            divIcon: vi.fn(),
            marker: vi.fn(() => ({
                addTo: vi.fn().mockReturnThis(),
                setLatLng: vi.fn().mockReturnThis(),
                setOpacity: vi.fn().mockReturnThis(),
                bindPopup: vi.fn().mockReturnThis(),
                on: vi.fn().mockReturnThis()
            })),
            circle: vi.fn(() => ({
                addTo: vi.fn().mockReturnThis(),
                setLatLng: vi.fn().mockReturnThis(),
                setRadius: vi.fn().mockReturnThis(),
                setStyle: vi.fn().mockReturnThis()
            })),
            featureGroup: vi.fn(() => ({
                addTo: vi.fn().mockReturnThis(),
                clearLayers: vi.fn().mockReturnThis(),
                addLayer: vi.fn().mockReturnThis()
            })),
            tileLayer: vi.fn(() => ({
                addTo: vi.fn().mockReturnThis()
            })),
            heatLayer: vi.fn(() => ({
                addTo: vi.fn().mockReturnThis()
            }))
        };
        global.ResizeObserver = vi.fn(() => ({
            observe: vi.fn(),
            disconnect: vi.fn()
        }));

        // Execute the script
        // heatmap.js defines variables with 'let' at the top level.
        // In a module, these won't become properties of 'global'.
        // We can wrap it or just use eval and hope for the best, 
        // but heatmap.js is not a module.
        
        // Clear previous state
        global.map = undefined;
        
        try {
            eval(heatmapJs);
        } catch (e) {
            console.error('Error evaluating heatmap.js', e);
        }
    });

    it('getMapState should return null if map is not initialized', () => {
        global.map = null;
        expect(window.getMapState()).toBeNull();
    });

    it('getMapState should return correct state when map is initialized', () => {
        // Initialize map via initHeatMap to set internal 'map' variable
        window.initHeatMap('test', 5, 15, [], {}, [], {}, false);

        // Get the map instance from the mock
        const mockMap = L.map.mock.results[0].value;
        
        const mockCenter = {
            lat: 5,
            lng: 15,
            distanceTo: vi.fn(() => 2000) // 2km
        };
        const mockBounds = {
            getNorth: () => 10,
            getEast: () => 20,
            getSouth: () => 0,
            getWest: () => 10
        };
        
        mockMap.getCenter = vi.fn(() => mockCenter);
        mockMap.getBounds = vi.fn(() => mockBounds);

        const state = window.getMapState();
        expect(state).toEqual({
            lat: 5,
            lng: 15,
            radiusKm: 1 // 2000 / 2000 = 1
        });
    });
});
