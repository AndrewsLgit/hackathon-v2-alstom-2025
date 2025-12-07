import React, { useState, useEffect } from 'react';
import { Block, GRID_COLS, GRID_ROWS } from './types';
import Controls from './components/Controls';
import TopView from './components/TopView';
import IsoView from './components/IsoView';
import ViewContainer from './components/ViewContainer';
import { Box, Layers } from 'lucide-react';

const App: React.FC = () => {
  // Raw input string for heights
  const [inputString, setInputString] = useState<string>('10, 20, 30, 15, 25, 40, 10, 5, 50, 20, 15, 30, 10, 10, 10');
  
  // Height Limit state (Global default)
  const [maxHeight, setMaxHeight] = useState<number>(100);
  
  // Custom Limits per slot: key "x-y", value: number
  const [customLimits, setCustomLimits] = useState<Record<string, number>>({});
  
  // Target Slots (Green Plan): key "x-y", value: boolean
  const [targetSlots, setTargetSlots] = useState<Record<string, boolean>>({});
  
  // Modes
  const [isLimitMode, setIsLimitMode] = useState<boolean>(false);
  const [isTargetMode, setIsTargetMode] = useState<boolean>(false);

  // The pool of blocks derived from input
  const [blocks, setBlocks] = useState<Block[]>([]);
  
  // Currently selected block ID to place
  const [selectedBlockId, setSelectedBlockId] = useState<string | null>(null);

  // Error state for animation feedback {x, y}
  const [errorSlot, setErrorSlot] = useState<{x: number, y: number} | null>(null);

  // Initialize blocks from default string on load
  useEffect(() => {
    parseInput(inputString);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const parseInput = (str: string) => {
    // Split by comma, space, or newline
    const values = str.split(/[\s,]+/).map(v => parseFloat(v)).filter(n => !isNaN(n) && n > 0);
    
    // Create block objects.
    const newBlocks: Block[] = values.map((h, index) => ({
      id: `block-${index}-${Date.now()}`, // Unique ID
      height: h,
      placedAt: null
    }));
    setBlocks(newBlocks);
    setSelectedBlockId(null);
  };

  const handleInputChange = (newStr: string) => {
    setInputString(newStr);
  };

  const handleApplyInput = () => {
    parseInput(inputString);
  };

  const handleSelectBlock = (id: string) => {
    if (isLimitMode || isTargetMode) return; // Disable block selection in paint modes
    setSelectedBlockId(id === selectedBlockId ? null : id);
  };

  const toggleLimitMode = () => {
      const newVal = !isLimitMode;
      setIsLimitMode(newVal);
      if (newVal) setIsTargetMode(false);
  };

  const toggleTargetMode = () => {
      const newVal = !isTargetMode;
      setIsTargetMode(newVal);
      if (newVal) setIsLimitMode(false);
  };

  const triggerError = (x: number, y: number) => {
    setErrorSlot({ x, y });
    // Clear error after animation duration (400ms)
    setTimeout(() => {
        setErrorSlot(null);
    }, 400);
  };

  const getLimitAt = (x: number, y: number) => {
      return customLimits[`${x}-${y}`] ?? maxHeight;
  };

  // --- Hangar (Import/Export) Logic ---

  const handleExportHangar = () => {
    const data = {
      timestamp: Date.now(),
      maxHeight,
      customLimits,
      targetSlots,
      inputString,
      // We also save current block positions if desired, or just the inventory
      blocks
    };

    const json = JSON.stringify(data, null, 2);
    const blob = new Blob([json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    
    const a = document.createElement('a');
    a.href = url;
    a.download = `hangar_terrain_${new Date().toISOString().slice(0,10)}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  const handleImportHangar = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (event) => {
      try {
        const json = event.target?.result as string;
        const data = JSON.parse(json);

        // Restore state safely
        if (typeof data.maxHeight === 'number') setMaxHeight(data.maxHeight);
        if (data.customLimits) setCustomLimits(data.customLimits);
        if (data.targetSlots) setTargetSlots(data.targetSlots);
        if (data.inputString) setInputString(data.inputString);
        if (data.blocks) setBlocks(data.blocks);
        
        // Reset modes
        setIsLimitMode(false);
        setIsTargetMode(false);
        alert("Hangar importé avec succès !");
      } catch (err) {
        console.error("Erreur d'importation", err);
        alert("Fichier Hangar invalide.");
      }
    };
    reader.readAsText(file);
    // Reset input value to allow re-importing same file if needed
    e.target.value = '';
  };

  // --- Interaction Logic ---

  const handleInteract = (x: number, y: number) => {
    const key = `${x}-${y}`;

    // SCENARIO 0.1: Target Painting Mode (with Toggle)
    if (isTargetMode) {
        setTargetSlots(prev => {
            const newState = { ...prev };
            if (newState[key]) {
                delete newState[key]; // Toggle OFF if already exists
            } else {
                newState[key] = true; // Toggle ON
            }
            return newState;
        });
        return;
    }

    // SCENARIO 0.2: Limit Painting Mode (with Toggle/Update)
    if (isLimitMode) {
        setCustomLimits(prev => {
            const newState = { ...prev };
            const currentVal = newState[key];
            
            // In limit mode, 'maxHeight' input currently acts as the brush value
            // If the slot already has THIS EXACT limit, remove it.
            if (currentVal === maxHeight) {
                delete newState[key];
            } else {
                // Otherwise set/update it
                newState[key] = maxHeight;
            }
            return newState;
        });
        return;
    }

    // Find all blocks currently at this position
    const stack = blocks.filter(b => b.placedAt?.x === x && b.placedAt?.y === y);
    
    // Sort by Z to find the top one
    stack.sort((a, b) => (a.placedAt?.z || 0) - (b.placedAt?.z || 0));
    
    const maxZ = stack.length > 0 ? stack[stack.length - 1].placedAt!.z : -1;
    const currentStackHeight = stack.reduce((sum, b) => sum + b.height, 0);
    const localLimit = getLimitAt(x, y);

    // SCENARIO 1: Placing a selected block from inventory
    if (selectedBlockId) {
      const blockToPlace = blocks.find(b => b.id === selectedBlockId);
      
      if (blockToPlace) {
          if (currentStackHeight + blockToPlace.height > localLimit) {
              triggerError(x, y);
              return;
          }

          const updatedBlocks = blocks.map(b => {
            if (b.id === selectedBlockId) {
               // Place on top of existing stack (maxZ + 1)
               return { ...b, placedAt: { x, y, z: maxZ + 1 } };
            }
            return b;
          });
          setBlocks(updatedBlocks);
          setSelectedBlockId(null); // Deselect after placing
      }
    } 
    // SCENARIO 2: Removing the top block (if nothing selected)
    else if (stack.length > 0) {
      // Remove the top-most block
      const topBlock = stack[stack.length - 1];
      const updatedBlocks = blocks.map(b => {
        if (b.id === topBlock.id) {
            return { ...b, placedAt: null };
        }
        return b;
      });
      setBlocks(updatedBlocks);
    }
  };

  const handleAutoStack = () => {
    const unplacedBlocks = blocks.filter(b => !b.placedAt);
    if (unplacedBlocks.length === 0) return;

    // Create a working copy of blocks to mutate virtually before setting state
    let workingBlocks = [...blocks];
    
    // Helper to get stack height at x,y
    const getHeightAt = (x: number, y: number) => {
        return workingBlocks
            .filter(b => b.placedAt?.x === x && b.placedAt?.y === y)
            .reduce((sum, b) => sum + b.height, 0);
    };

    // Helper to get max Z at x,y
    const getMaxZAt = (x: number, y: number) => {
        const stack = workingBlocks.filter(b => b.placedAt?.x === x && b.placedAt?.y === y);
        if (stack.length === 0) return -1;
        stack.sort((a, b) => (a.placedAt?.z || 0) - (b.placedAt?.z || 0));
        return stack[stack.length - 1].placedAt!.z;
    };

    // 1. Generate all possible grid slots
    const allSlots: {x: number, y: number}[] = [];
    for (let y = 0; y < GRID_ROWS; y++) {
        for (let x = 0; x < GRID_COLS; x++) {
            allSlots.push({ x, y });
        }
    }

    // 2. Sort slots: TARGET slots (green) come first
    allSlots.sort((a, b) => {
        const isTargetA = targetSlots[`${a.x}-${a.y}`] ? 1 : 0;
        const isTargetB = targetSlots[`${b.x}-${b.y}`] ? 1 : 0;
        return isTargetB - isTargetA; // Descending: 1 (Target) before 0 (Non-target)
    });

    // 3. Iterate through available blocks and try to place them
    unplacedBlocks.forEach(block => {
        // Iterate through prioritised slots
        for (const slot of allSlots) {
            const { x, y } = slot;
            const currentH = getHeightAt(x, y);
            const localLimit = getLimitAt(x, y);
            
            if (currentH + block.height <= localLimit) {
                const z = getMaxZAt(x, y) + 1;
                
                // Update the block in our working set
                workingBlocks = workingBlocks.map(b => 
                    b.id === block.id 
                    ? { ...b, placedAt: { x, y, z } }
                    : b
                );
                // Break inner loop to move to next block
                break; 
            }
        }
    });

    setBlocks(workingBlocks);
  };

  return (
    <div className="min-h-screen flex flex-col font-sans bg-slate-100">
      {/* Header */}
      <header className="bg-white border-b border-slate-200 px-6 py-4 flex items-center shadow-sm z-10">
        <div className="p-2 bg-blue-600 rounded-lg text-white mr-3">
          <Box size={24} />
        </div>
        <div>
          <h1 className="text-xl font-bold text-slate-800">Poseur de Pavés 3D</h1>
          <p className="text-sm text-slate-500">Grille 20x20 • Empilement de pavés • Vue Isométrique & Plan</p>
        </div>
      </header>

      <main className="flex-1 flex flex-col lg:flex-row overflow-hidden">
        {/* Controls Sidebar - Dark Theme */}
        <aside className="w-full lg:w-80 bg-slate-900 border-r border-slate-700 overflow-y-auto z-20 shadow-lg flex flex-col shrink-0">
          <Controls 
            inputString={inputString}
            onInputChange={handleInputChange}
            onApply={handleApplyInput}
            blocks={blocks}
            selectedBlockId={selectedBlockId}
            onSelectBlock={handleSelectBlock}
            maxHeight={maxHeight}
            onMaxHeightChange={setMaxHeight}
            onAutoStack={handleAutoStack}
            isLimitMode={isLimitMode}
            onToggleLimitMode={toggleLimitMode}
            isTargetMode={isTargetMode}
            onToggleTargetMode={toggleTargetMode}
            onExportHangar={handleExportHangar}
            onImportHangar={handleImportHangar}
          />
        </aside>

        {/* Visualization Area */}
        <div className="flex-1 bg-slate-50 p-4 lg:p-6 overflow-y-auto">
          <div className="grid grid-cols-1 xl:grid-cols-2 gap-6 h-full min-h-[600px]">
            
            {/* 2D View */}
            <ViewContainer title="Vue de Haut (2D)" icon={<Layers className="w-4 h-4" />}>
              <TopView 
                rows={GRID_ROWS} 
                cols={GRID_COLS} 
                blocks={blocks} 
                onSlotClick={handleInteract}
                highlightedBlockId={selectedBlockId}
                maxHeight={maxHeight}
                customLimits={customLimits}
                targetSlots={targetSlots}
                errorSlot={errorSlot}
                isLimitMode={isLimitMode}
                isTargetMode={isTargetMode}
              />
            </ViewContainer>

            {/* Iso View */}
            <ViewContainer title="Vue Isométrique (3D)" icon={<Box className="w-4 h-4" />}>
              <IsoView 
                 rows={GRID_ROWS} 
                 cols={GRID_COLS} 
                 blocks={blocks} 
                 onSlotClick={handleInteract}
                 highlightedBlockId={selectedBlockId}
                 maxHeight={maxHeight}
                 customLimits={customLimits}
                 targetSlots={targetSlots}
                 errorSlot={errorSlot}
                 isLimitMode={isLimitMode}
                 isTargetMode={isTargetMode}
              />
            </ViewContainer>

          </div>
        </div>
      </main>
    </div>
  );
};

export default App;