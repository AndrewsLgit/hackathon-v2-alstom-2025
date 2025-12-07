import React, { useMemo, useState, useRef } from 'react';
import { Block } from '../types';
import { ZoomIn, ZoomOut, RotateCcw } from 'lucide-react';

interface IsoViewProps {
  rows: number;
  cols: number;
  blocks: Block[];
  onSlotClick: (x: number, y: number) => void;
  highlightedBlockId: string | null;
  maxHeight: number;
  customLimits: Record<string, number>;
  targetSlots: Record<string, boolean>;
  errorSlot: { x: number, y: number } | null;
  isLimitMode: boolean;
  isTargetMode: boolean;
}

// Solid colors for stacking
const COLORS = [
  '#2563eb', // Blue
  '#059669', // Emerald
  '#d97706', // Amber
  '#e11d48', // Rose
  '#7c3aed', // Violet
];
const STROKE_COLOR = '#1e293b'; // Slate-800
const HIGHLIGHT_COLOR = 'rgba(59, 130, 246, 0.4)';
const LIMIT_MODE_HIGHLIGHT = 'rgba(239, 68, 68, 0.4)'; // Reddish
const TARGET_MODE_HIGHLIGHT = 'rgba(16, 185, 129, 0.6)'; // Greenish

const GHOST_STROKE = 'rgba(255, 255, 255, 0.6)';

// Tile geometry
const TILE_WIDTH = 30;
const TILE_HEIGHT = 15; // Isometric compression
const BLOCK_SCALE = 1.0; // Vertical scale for height units (1 unit height = 1 pixel approx)

const IsoView: React.FC<IsoViewProps> = ({ 
    rows, 
    cols, 
    blocks, 
    onSlotClick, 
    highlightedBlockId,
    maxHeight,
    customLimits,
    targetSlots,
    errorSlot,
    isLimitMode,
    isTargetMode
}) => {
  const [zoom, setZoom] = useState(1);
  const [pan, setPan] = useState({ x: 0, y: 0 });
  const [hoveredSlot, setHoveredSlot] = useState<{x: number, y: number} | null>(null);
  
  // Interaction refs
  const isMouseDownRef = useRef(false);
  const isPanningRef = useRef(false);
  const lastMousePosRef = useRef<{ x: number, y: number } | null>(null);
  const dragStartPosRef = useRef<{ x: number, y: number } | null>(null);

  const svgRef = useRef<SVGSVGElement>(null);

  // Transform (Grid -> Iso)
  const toIso = (gridX: number, gridY: number) => {
    return {
      x: (gridX - gridY) * TILE_WIDTH,
      y: (gridX + gridY) * TILE_HEIGHT,
    };
  };

  // Center the grid initially
  const initialOffset = useMemo(() => {
    const top = toIso(0, 0);
    const bottom = toIso(cols - 1, rows - 1);
    const left = toIso(0, rows - 1);
    const right = toIso(cols - 1, 0);
    
    const width = right.x - left.x;
    const height = bottom.y - top.y;
    
    return { x: -width / 2, y: -height / 2 - 200 };
  }, [cols, rows]);

  // Handle Zoom
  const handleZoom = (delta: number) => {
    setZoom((z) => Math.max(0.1, Math.min(3, z + delta)));
  };

  // Mouse Handlers
  const handleMouseDown = (e: React.MouseEvent) => {
    isMouseDownRef.current = true;
    isPanningRef.current = false;
    
    lastMousePosRef.current = { x: e.clientX, y: e.clientY };
    dragStartPosRef.current = { x: e.clientX, y: e.clientY };
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    // Handle Pan logic
    if (isMouseDownRef.current && lastMousePosRef.current) {
        const dx = e.clientX - lastMousePosRef.current.x;
        const dy = e.clientY - lastMousePosRef.current.y;

        // Check threshold to enable panning state
        if (!isPanningRef.current && dragStartPosRef.current) {
            const moveDist = Math.sqrt(
                Math.pow(e.clientX - dragStartPosRef.current.x, 2) + 
                Math.pow(e.clientY - dragStartPosRef.current.y, 2)
            );
            if (moveDist > 5) {
                isPanningRef.current = true;
            }
        }

        if (isPanningRef.current) {
            // Only pan if NOT in a Paint Mode OR if holding shift (override)
            // But generally, panning should work on background drag, while slot drag should paint.
            // Current logic: Background drag pans. Slot click+drag handles painting.
            setPan((p) => ({ x: p.x - dx / zoom, y: p.y - dy / zoom }));
        }
        lastMousePosRef.current = { x: e.clientX, y: e.clientY };
    }
  };

  const handleMouseUp = () => {
    isMouseDownRef.current = false;
    lastMousePosRef.current = null;
    dragStartPosRef.current = null;
    
    // Slight delay to prevent race condition with onClick
    setTimeout(() => {
        isPanningRef.current = false;
    }, 50);
  };

  const handleSlotMouseEnter = (x: number, y: number) => {
      setHoveredSlot({x, y});
      // "Paint" limits/targets if dragging in mode
      if ((isLimitMode || isTargetMode) && isMouseDownRef.current && !isPanningRef.current) {
          onSlotClick(x, y);
      }
  };

  const handleSlotClick = (e: React.MouseEvent, x: number, y: number) => {
      if (isPanningRef.current) {
          e.stopPropagation();
          return;
      }
      onSlotClick(x, y);
  };

  // Drawing helpers
  const drawCube = (x: number, y: number, z: number, height: number, color: string, isBase = false, opacity = 1) => {
    const iso = toIso(x, y);
    const bottomY = iso.y - z * BLOCK_SCALE;
    const topY = bottomY - height * BLOCK_SCALE;

    // Top Face
    const top = `M${iso.x},${topY} L${iso.x + TILE_WIDTH},${topY + TILE_HEIGHT} L${iso.x},${topY + 2 * TILE_HEIGHT} L${iso.x - TILE_WIDTH},${topY + TILE_HEIGHT} Z`;
    // Right Face
    const right = `M${iso.x},${topY + 2 * TILE_HEIGHT} L${iso.x + TILE_WIDTH},${topY + TILE_HEIGHT} L${iso.x + TILE_WIDTH},${bottomY + TILE_HEIGHT} L${iso.x},${bottomY + 2 * TILE_HEIGHT} Z`;
    // Left Face
    const left = `M${iso.x},${topY + 2 * TILE_HEIGHT} L${iso.x - TILE_WIDTH},${topY + TILE_HEIGHT} L${iso.x - TILE_WIDTH},${bottomY + TILE_HEIGHT} L${iso.x},${bottomY + 2 * TILE_HEIGHT} Z`;

    return (
      <g stroke={STROKE_COLOR} strokeWidth="0.5" opacity={opacity}>
        <path d={left} fill={color} filter="brightness(0.8)" />
        <path d={right} fill={color} filter="brightness(0.6)" />
        <path d={top} fill={color} />
      </g>
    );
  };

  const drawGhostBox = (x: number, y: number) => {
      const iso = toIso(x, y);
      const localLimit = customLimits[`${x}-${y}`] ?? maxHeight;
      
      const totalH = localLimit * BLOCK_SCALE;
      const topY = iso.y - totalH;
      const bottomY = iso.y;

      const top = `M${iso.x},${topY} L${iso.x + TILE_WIDTH},${topY + TILE_HEIGHT} L${iso.x},${topY + 2 * TILE_HEIGHT} L${iso.x - TILE_WIDTH},${topY + TILE_HEIGHT} Z`;
      const right = `M${iso.x},${topY + 2 * TILE_HEIGHT} L${iso.x + TILE_WIDTH},${topY + TILE_HEIGHT} L${iso.x + TILE_WIDTH},${bottomY + TILE_HEIGHT} L${iso.x},${bottomY + 2 * TILE_HEIGHT}`; 
      const left = `M${iso.x},${topY + 2 * TILE_HEIGHT} L${iso.x - TILE_WIDTH},${topY + TILE_HEIGHT} L${iso.x - TILE_WIDTH},${bottomY + TILE_HEIGHT} L${iso.x},${bottomY + 2 * TILE_HEIGHT}`; 
      const vLine1 = `M${iso.x},${topY + 2*TILE_HEIGHT} L${iso.x},${bottomY + 2*TILE_HEIGHT}`;
      
      const isCustom = customLimits[`${x}-${y}`] !== undefined;

      return (
          <g 
            stroke={isCustom ? "rgba(239, 68, 68, 0.8)" : GHOST_STROKE} 
            strokeWidth={isCustom ? "1.5" : "1"} 
            strokeDasharray="4 2" 
            fill="none" 
            className="pointer-events-none"
          >
              {/* Filled Top Face (Ceiling Limit) */}
              <path 
                d={top} 
                fill={isCustom ? "rgba(239, 68, 68, 0.3)" : "rgba(220, 38, 38, 0.15)"} 
                stroke={isCustom ? "rgba(239, 68, 68, 0.8)" : "rgba(220, 38, 38, 0.4)"}
                strokeDasharray="none" 
              />
              <path d={right} />
              <path d={left} />
              <path d={vLine1} />
              {isCustom && (
                  <text 
                    x={iso.x} 
                    y={topY - 5} 
                    textAnchor="middle" 
                    fill="#ef4444" 
                    fontSize="12" 
                    fontWeight="bold"
                    stroke="none"
                    filter="drop-shadow(0 1px 1px white)"
                  >
                      {localLimit}
                  </text>
              )}
          </g>
      );
  };

  const drawGlobalCeiling = () => {
    // Only draw if NOT in limit/target mode, or fade it out
    const opacity = (isLimitMode || isTargetMode) ? 0.1 : 1;
    const zOffset = maxHeight * BLOCK_SCALE;
    
    // Corners of the entire grid (0,0) to (cols, rows)
    const pt00 = toIso(0, 0);
    const ptC0 = toIso(cols, 0); 
    const ptCR = toIso(cols, rows); 
    const pt0R = toIso(0, rows); 
    
    const t00 = { x: pt00.x, y: pt00.y - zOffset };
    const tC0 = { x: ptC0.x, y: ptC0.y - zOffset };
    const tCR = { x: ptCR.x, y: ptCR.y - zOffset };
    const t0R = { x: pt0R.x, y: pt0R.y - zOffset };
    
    return (
        <g className="pointer-events-none transition-opacity duration-300" opacity={opacity}>
            {/* Ceiling Plane */}
            <path 
                d={`M${t00.x},${t00.y} L${tC0.x},${tC0.y} L${tCR.x},${tCR.y} L${t0R.x},${t0R.y} Z`}
                fill="rgba(200, 210, 255, 0.08)"
                stroke="rgba(59, 130, 246, 0.3)"
                strokeWidth="1.5"
                strokeDasharray="6 4"
            />
        </g>
    );
  };

  const drawRuler = () => {
      const iso = toIso(0, rows-1);
      const startX = iso.x - TILE_WIDTH - 20;
      const startY = iso.y + TILE_HEIGHT;
      const heightPx = maxHeight * BLOCK_SCALE;

      const ticks = [];
      const step = 20;
      for(let h = 0; h <= maxHeight; h += step) {
          const y = startY - h * BLOCK_SCALE;
          ticks.push(
              <g key={h}>
                  <line x1={startX} y1={y} x2={startX + 10} y2={y} stroke="black" strokeWidth="1" />
                  <text x={startX - 5} y={y + 4} textAnchor="end" fontSize="10" fill="#333">{h}</text>
              </g>
          );
      }
      
      return (
          <g className="select-none pointer-events-none">
              <line x1={startX} y1={startY} x2={startX} y2={startY - heightPx} stroke="black" strokeWidth="1" />
              {ticks}
          </g>
      );
  };

  // Render loop
  const renderGrid = () => {
    const elements = [];
    
    // Render back-to-front
    for (let x = 0; x < cols; x++) {
      for (let y = 0; y < rows; y++) {
        const iso = toIso(x, y);
        const stack = blocks
           .filter(b => b.placedAt?.x === x && b.placedAt?.y === y)
           .sort((a, b) => (a.placedAt?.z || 0) - (b.placedAt?.z || 0));
        
        const isHovered = hoveredSlot?.x === x && hoveredSlot?.y === y;
        const isError = errorSlot?.x === x && errorSlot?.y === y;
        
        // Target Mode visuals
        const isTargeted = targetSlots[`${x}-${y}`];
        // Custom limit visual
        const hasCustomLimit = customLimits[`${x}-${y}`] !== undefined;

        // Determine Base Tile Colors
        let baseFill = '#f8fafc';
        let baseStroke = '#cbd5e1';

        if (isTargeted) {
            baseFill = '#a7f3d0'; // Emerald 200
            baseStroke = '#34d399'; // Emerald 400
        } else if (hasCustomLimit) {
            baseFill = '#fef2f2';
            baseStroke = '#fca5a5';
        }
        
        if (isHovered && !isTargetMode && !isLimitMode) {
             baseFill = '#e2e8f0';
             baseStroke = '#94a3b8';
        }

        // Base tile
        elements.push(
          <g 
            key={`base-${x}-${y}`}
            onClick={(e) => handleSlotClick(e, x, y)}
            onMouseEnter={() => handleSlotMouseEnter(x, y)}
            className={`cursor-pointer transition-opacity ${isError ? 'shake-error' : ''}`}
          >
            <path 
                d={`M${iso.x},${iso.y} L${iso.x + TILE_WIDTH},${iso.y + TILE_HEIGHT} L${iso.x},${iso.y + 2 * TILE_HEIGHT} L${iso.x - TILE_WIDTH},${iso.y + TILE_HEIGHT} Z`}
                fill={baseFill}
                stroke={baseStroke}
                strokeWidth={(isHovered || isTargeted) ? 2 : 1}
            />
            {/* Highlight for modes on hover */}
            {isHovered && (
                 <path 
                 d={`M${iso.x},${iso.y} L${iso.x + TILE_WIDTH},${iso.y + TILE_HEIGHT} L${iso.x},${iso.y + 2 * TILE_HEIGHT} L${iso.x - TILE_WIDTH},${iso.y + TILE_HEIGHT} Z`}
                 fill={isLimitMode ? LIMIT_MODE_HIGHLIGHT : (isTargetMode ? TARGET_MODE_HIGHLIGHT : (highlightedBlockId ? HIGHLIGHT_COLOR : 'transparent'))}
                 className="pointer-events-none"
             />
            )}
          </g>
        );

        // Render Stack
        let accumulatedHeight = 0;
        stack.forEach((block, index) => {
            const zIndex = block.placedAt?.z || 0;
            const color = COLORS[zIndex % COLORS.length];
            
            elements.push(
                <g 
                    key={block.id} 
                    className={`cursor-pointer ${isError && index === stack.length-1 ? 'shake-error' : ''}`}
                    onClick={(e) => handleSlotClick(e, x, y)} 
                    onMouseEnter={() => handleSlotMouseEnter(x, y)}
                >
                    {drawCube(x, y, accumulatedHeight, block.height, color)}
                    
                    <text
                        x={iso.x}
                        y={iso.y - accumulatedHeight * BLOCK_SCALE - block.height * BLOCK_SCALE + TILE_HEIGHT * 1.2}
                        textAnchor="middle"
                        fill="white"
                        fontSize="10"
                        fontWeight="bold"
                        className="pointer-events-none select-none"
                        style={{ 
                            textShadow: '1px 1px 2px rgba(0,0,0,0.8)',
                            filter: 'drop-shadow(0px 1px 1px rgba(0,0,0,0.8))'
                        }}
                    >
                        {block.height}
                    </text>
                </g>
            );
            accumulatedHeight += block.height;
        });

        // Ghost Box on Hover (only if hovered and NOT target mode, target mode focuses on floor)
        if (isHovered && !isTargetMode) {
             elements.push(<g key={`ghost-${x}-${y}`}>{drawGhostBox(x, y)}</g>);
        }
      }
    }
    return elements;
  };

  const viewBoxWidth = 1000 / zoom;
  const viewBoxHeight = 800 / zoom;
  const viewBoxX = initialOffset.x + pan.x - viewBoxWidth / 2;
  const viewBoxY = initialOffset.y + pan.y - viewBoxHeight / 2;

  return (
    <div className="relative w-full h-full bg-slate-100 overflow-hidden cursor-move">
      {/* Zoom Controls */}
      <div className="absolute top-4 right-4 flex flex-col gap-2 z-10">
        <button onClick={() => handleZoom(0.2)} className="p-2 bg-white rounded shadow hover:bg-slate-50"><ZoomIn size={20} /></button>
        <button onClick={() => setZoom(1)} className="p-2 bg-white rounded shadow hover:bg-slate-50"><RotateCcw size={20} /></button>
        <button onClick={() => handleZoom(-0.2)} className="p-2 bg-white rounded shadow hover:bg-slate-50"><ZoomOut size={20} /></button>
      </div>

      <svg 
        ref={svgRef}
        width="100%" 
        height="100%" 
        viewBox={`${viewBoxX} ${viewBoxY} ${viewBoxWidth} ${viewBoxHeight}`}
        onMouseDown={handleMouseDown}
        onMouseMove={handleMouseMove}
        onMouseUp={handleMouseUp}
        onMouseLeave={handleMouseUp}
        className="touch-none"
      >
        <g>
            {drawRuler()}
            {renderGrid()}
            {drawGlobalCeiling()}
        </g>
      </svg>
    </div>
  );
};

export default IsoView;