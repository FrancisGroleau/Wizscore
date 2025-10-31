// Define the W polygon as an array of [x, y] in percentages
const wPolygon = [
    [38, 44], [26, 0], [0, 0], [17, 100], [34, 100], [53, 74],
    [69, 100], [86, 100], [100, 0], [76, 0], [67, 44], [52, 28]
];

// Threshold in percentage units (tune as needed)
const CHECKPOINT_RADIUS = 50;

// Helper: distance between two points
function distance(a, b) {
    return Math.sqrt((a.x - b[0]) ** 2 + (a.y - b[1]) ** 2);
}

// Check if checkpoints are hit in order
function checkTrace(trace, checkpoints) {
    let checkpointIdx = 0;
    for (const pt of trace) {
        if (distance(pt, checkpoints[checkpointIdx]) < CHECKPOINT_RADIUS) {
            checkpointIdx++;
            if (checkpointIdx === checkpoints.length) return true;
        }
    }
    return false;
}

function isPointInPolygon(x, y, polygon) {
    let inside = false;
    for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
        const xi = polygon[i][0], yi = polygon[i][1];
        const xj = polygon[j][0], yj = polygon[j][1];
        const intersect = ((yi > y) !== (yj > y)) &&
            (x < (xj - xi) * (y - yi) / (yj - yi) + xi);
        if (intersect) inside = !inside;
    }
    return inside;
}

const overlay = document.querySelector('.w-cutout-overlay');
let isDrawing = false;
let tracedPoints = [];

function getPointerPosition(e, rect) {
    let clientX, clientY;
    if (e.touches && e.touches.length > 0) {
        clientX = e.touches[0].clientX;
        clientY = e.touches[0].clientY;
    } else {
        clientX = e.clientX;
        clientY = e.clientY;
    }
    const x = ((clientX - rect.left) / rect.width) * 100;
    const y = ((clientY - rect.top) / rect.height) * 100;
    return { x, y };
}

function handleStart(e) {
    isDrawing = true;
    tracedPoints = [];
    e.preventDefault();
}

function handleMove(e) {
    if (!isDrawing) return;
    const rect = overlay.getBoundingClientRect();
    const { x, y } = getPointerPosition(e, rect);
    if (isPointInPolygon(x, y, wPolygon)) {
        tracedPoints.push({ x, y });
    }
    e.preventDefault();
}

function handleEnd(e) {
    isDrawing = false;
    if (checkTrace(tracedPoints, wPolygon)) {
        const anchor = document.getElementById('goToSnake');
        if (anchor) {
            window.location = anchor.href;
        }
    } 
    e.preventDefault();
}

overlay.addEventListener('mousedown', handleStart);
overlay.addEventListener('mousemove', handleMove);
overlay.addEventListener('mouseup', handleEnd);

overlay.addEventListener('touchstart', handleStart, { passive: false });
overlay.addEventListener('touchmove', handleMove, { passive: false });
overlay.addEventListener('touchend', handleEnd, { passive: false });