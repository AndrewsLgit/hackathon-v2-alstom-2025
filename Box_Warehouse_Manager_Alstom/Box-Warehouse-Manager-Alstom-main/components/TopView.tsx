import React, { useRef } from 'react';
import { Block } from '../types';

interface TopViewProps {
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

// Colors matching IsoView exactly
const COLORS = [
  '#2563eb', // Blue
  '#059669', // Emerald
  '#d97706', // Amber
  '#e11d48', // Rose
  '#7c3aed', // Violet
];

// Stroke colors matching IsoView
const STROKE_BLOCK = '#1e293b'; // Slate-800
const STROKE_BASE = '#cbd5e1';  // Slate-300
const STROKE_TARGET = '#34d399'; // Emerald-400
const STROKE_LIMIT = '#fca5a5';  // Red-300

// Fill colors matching IsoView
const FILL_BASE = '#f8fafc';    // Slate-50
const FILL_TARGET = '#a7f3d0';  // Emerald-200
const FILL_LIMIT = '#fef2f2';   // Red-50
const FILL_HOVER = '#e2e8f0';   // Slate-200

const TopView: React.FC<TopViewProps> = ({ 
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
  const isMouseDownRef = useRef(false);

  // Generate grid coordinates
  const grid = [];
  for (let y = 0; y < rows; y++) {
    for (let x = 0; x < cols; x++) {
      grid.push({ x, y });
    }
  }

  // Helper to find stack at x,y
  const getStackAt = (x: number, y: number) => {
    const stack = blocks.filter(b => b.placedAt?.x === x && b.placedAt?.y === y);
    // Sort ascending by Z
    return stack.sort((a, b) => (a.placedAt?.z || 0) - (b.placedAt?.z || 0));
  };

  const handleMouseDown = (x: number, y: number) => {
      isMouseDownRef.current = true;
      onSlotClick(x, y);
  };

  const handleMouseEnter = (x: number, y: number) => {
      // Paint if dragging in a mode
      if (isMouseDownRef.current && (isLimitMode || isTargetMode)) {
          onSlotClick(x, y);
      }
  };

  const handleMouseUp = () => {
      isMouseDownRef.current = false;
  };

  return (
    <div 
        className="w-full h-full flex items-center justify-center overflow-auto p-4 bg-slate-100"
        onMouseUp={handleMouseUp}
        onMouseLeave={handleMouseUp}
    >
      <div 
        className="grid select-none shadow-xl bg-white"
        style={{
          gridTemplateColumns: `repeat(${cols}, minmax(24px, 1fr))`,
          gridTemplateRows: `repeat(${rows}, minmax(24px, 1fr))`,
          minWidth: 'fit-content',
          padding: '1px', // Outer padding
        }}
      >
        {grid.map(({ x, y }) => {
          const stack = getStackAt(x, y);
          const topBlock = stack.length > 0 ? stack[stack.length - 1] : null;
          const currentHeight = stack.reduce((acc, b) => acc + b.height, 0);
          const zIndex = topBlock?.placedAt?.z || 0;
          
          // Check custom limit
          const localLimit = customLimits[`${x}-${y}`];
          const hasCustomLimit = localLimit !== undefined;
          const effectiveLimit = hasCustomLimit ? localLimit : maxHeight;

          // Check target slot
          const isTargeted = targetSlots[`${x}-${y}`];

          // Check if this slot is in error state
          const isError = errorSlot?.x === x && errorSlot?.y === y;

          // Determine styles based on state
          let bg = FILL_BASE;
          let borderColor = STROKE_BASE;
          let textColor = 'white';

          if (isTargeted) {
              bg = FILL_TARGET;
              borderColor = STROKE_TARGET;
          } else if (hasCustomLimit) {
              bg = FILL_LIMIT;
              borderColor = STROKE_LIMIT;
          }

          if (topBlock) {
             bg = COLORS[zIndex % COLORS.length];
             borderColor = STROKE_BLOCK;
          }

          // Frame style for Custom Limits (Top View specific representation)
          const limitBorderClass = (hasCustomLimit && !isError && !topBlock) 
            ? 'ring-inset ring-2 ring-red-300' 
            : '';

          return (
            <div
              key={`${x}-${y}`}
              onMouseDown={() => handleMouseDown(x, y)}
              onMouseEnter={() => handleMouseEnter(x, y)}
              className={`
                aspect-square relative flex items-center justify-center cursor-pointer transition-all duration-75
                ${isError ? 'shake-error z-50 ring-4 ring-red-500' : ''}
                ${!topBlock && !isError ? 'hover:bg-slate-200' : ''}
                ${isLimitMode ? 'hover:ring-2 hover:ring-red-400 hover:z-20' : ''}
                ${isTargetMode ? 'hover:ring-2 hover:ring-emerald-400 hover:z-20' : ''}
              `}
              style={{
                  backgroundColor: bg,
                  border: `1px solid ${borderColor}`,
                  // If top block, we want sharp borders like the SVG cubes
              }}
              title={`Pos: (${x}, ${y})\nEmplacement: ${isTargeted ? 'Oui' : 'Non'}\nPavés: ${stack.length}\nHauteur: ${currentHeight} / ${effectiveLimit}${hasCustomLimit ? ' (Limite Locale)' : ' (Global)'}`}
            >
              {/* Content */}
              {topBlock ? (
                <span 
                    className="text-[10px] font-bold select-none z-10"
                    style={{ 
                        color: textColor,
                        filter: 'drop-shadow(0px 1px 1px rgba(0,0,0,0.8))'
                    }}
                  >
                    {topBlock.height}
                  </span>
              ) : (
                 // Empty Slot Indicators
                 <>
                    {/* Visual cue for Limit Mode (Red border is handled via style/ring, maybe add text?) */}
                    {hasCustomLimit && (
                        <span className="text-[8px] font-bold text-red-400 absolute top-0.5 right-0.5 leading-none">
                            L
                        </span>
                    )}
                 </>
              )}
              
              {/* Highlight Overlay (Blue selection) */}
              {!topBlock && highlightedBlockId && !isError && !isLimitMode && !isTargetMode && (
                 <div className="absolute inset-0 bg-blue-500/20 pointer-events-none"></div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};

export default TopView;