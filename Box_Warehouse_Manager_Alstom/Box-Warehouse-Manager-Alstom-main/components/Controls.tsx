import React, { useRef, useMemo } from 'react';
import { Block } from '../types';
import { RotateCcw, ArrowRight, Wand2, ShieldBan, LandPlot, Download, Upload, Archive, AlertTriangle } from 'lucide-react';

interface ControlsProps {
  inputString: string;
  onInputChange: (val: string) => void;
  onApply: () => void;
  blocks: Block[];
  selectedBlockId: string | null;
  onSelectBlock: (id: string) => void;
  maxHeight: number;
  onMaxHeightChange: (val: number) => void;
  onAutoStack: () => void;
  isLimitMode: boolean;
  onToggleLimitMode: () => void;
  isTargetMode: boolean;
  onToggleTargetMode: () => void;
  onExportHangar: () => void;
  onImportHangar: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

const Controls: React.FC<ControlsProps> = ({
  inputString,
  onInputChange,
  onApply,
  blocks,
  selectedBlockId,
  onSelectBlock,
  maxHeight,
  onMaxHeightChange,
  onAutoStack,
  isLimitMode,
  onToggleLimitMode,
  isTargetMode,
  onToggleTargetMode,
  onExportHangar,
  onImportHangar
}) => {
  const fileInputRef = useRef<HTMLInputElement>(null);
  
  const placedBlocks = blocks.filter(b => b.placedAt);
  const unplacedBlocks = blocks.filter(b => !b.placedAt);

  // 1. Sort blocks by height (Ascending)
  const sortedUnplaced = useMemo(() => {
    return [...unplacedBlocks].sort((a, b) => a.height - b.height);
  }, [unplacedBlocks]);

  // 2. Create two variables (Categories)
  // standardBlocks: Fit within global max height
  // oversizedBlocks: Exceed global max height (require specific limits)
  const standardBlocks = sortedUnplaced.filter(b => b.height <= maxHeight);
  const oversizedBlocks = sortedUnplaced.filter(b => b.height > maxHeight);

  const isAnyPaintMode = isLimitMode || isTargetMode;

  const renderBlockGrid = (blockList: Block[], title: string, isWarning = false) => (
    <div className="space-y-2">
       <h3 className={`text-xs font-bold uppercase tracking-wide mb-2 flex items-center gap-2 ${isWarning ? 'text-amber-400' : 'text-slate-400'}`}>
          {isWarning && <AlertTriangle size={12} />}
          {title} <span className="text-slate-500">({blockList.length})</span>
       </h3>
       <div className="grid grid-cols-3 gap-3">
          {blockList.map((block) => (
            <button
              key={block.id}
              onClick={() => onSelectBlock(block.id)}
              className={`
                relative group flex flex-col items-center justify-center p-2 rounded-xl border-2 transition-all duration-200
                ${selectedBlockId === block.id 
                  ? 'border-blue-500 bg-slate-800 shadow-md scale-105 ring-1 ring-blue-500' 
                  : `bg-slate-800 hover:bg-slate-700 ${isWarning ? 'border-amber-900/50 hover:border-amber-500/50' : 'border-slate-600 hover:border-slate-500'}`
                }
              `}
            >
              <div className={`w-full h-8 rounded shadow-sm flex items-center justify-center ${
                  isWarning 
                  ? 'bg-gradient-to-br from-amber-600 to-orange-700' 
                  : 'bg-gradient-to-br from-indigo-600 to-blue-700'
              }`}>
                 <span className="text-white font-bold text-xs drop-shadow-md">{block.height}</span>
              </div>
              
              {selectedBlockId === block.id && (
                  <div className="absolute -top-2 -right-2 bg-blue-500 text-white rounded-full p-0.5 shadow-sm">
                      <ArrowRight size={10} />
                  </div>
              )}
            </button>
          ))}
       </div>
    </div>
  );

  return (
    <div className="p-6 space-y-8 text-slate-200">
      
      {/* Configuration Section */}
      <div className="space-y-4">
        <h2 className="text-sm font-bold text-white uppercase tracking-wide border-b border-slate-700 pb-2">Outils & Modes</h2>
        
        {/* Mode Toggles */}
        <div className="space-y-2">
            {/* Limit Mode */}
            <button
                onClick={onToggleLimitMode}
                className={`w-full flex items-center justify-between px-4 py-3 rounded-lg border-2 transition-all ${
                    isLimitMode 
                    ? 'bg-red-900/30 border-red-500 text-red-100' 
                    : 'bg-slate-800 border-slate-600 hover:bg-slate-700 text-slate-300'
                }`}
            >
                <div className="flex items-center gap-3">
                    <ShieldBan size={20} className={isLimitMode ? "text-red-400" : "text-slate-400"} />
                    <div className="flex flex-col items-start text-left">
                        <span className={`font-bold text-sm ${isLimitMode ? 'text-red-400' : ''}`}>Mode Limite de Hauteur</span>
                    </div>
                </div>
                {isLimitMode && <div className="w-2 h-2 rounded-full bg-red-500 animate-pulse"></div>}
            </button>

            {/* Target Mode */}
            <button
                onClick={onToggleTargetMode}
                className={`w-full flex items-center justify-between px-4 py-3 rounded-lg border-2 transition-all ${
                    isTargetMode 
                    ? 'bg-emerald-900/30 border-emerald-500 text-emerald-100' 
                    : 'bg-slate-800 border-slate-600 hover:bg-slate-700 text-slate-300'
                }`}
            >
                <div className="flex items-center gap-3">
                    <LandPlot size={20} className={isTargetMode ? "text-emerald-400" : "text-slate-400"} />
                    <div className="flex flex-col items-start text-left">
                        <span className={`font-bold text-sm ${isTargetMode ? 'text-emerald-400' : ''}`}>Mode Emplacements</span>
                    </div>
                </div>
                {isTargetMode && <div className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></div>}
            </button>
        </div>

        {/* Info Text */}
        {!isAnyPaintMode && (
             <div className="text-[10px] text-slate-400 text-center italic">
                 Mode Construction Actif (Normal)
             </div>
        )}

        {/* Max Height / Brush Value */}
        <div className={`space-y-2 p-3 rounded-lg border transition-colors ${isLimitMode ? 'bg-red-900/10 border-red-500/30' : 'border-transparent'}`}>
            <label htmlFor="maxHeight" className={`block text-sm font-semibold transition-colors ${isLimitMode ? 'text-red-300' : 'text-slate-300'}`}>
                {isLimitMode ? 'Valeur Limite (Pinceau)' : 'Hauteur Max Global'}
            </label>
            <div className="flex items-center gap-2">
                <input 
                    type="number" 
                    id="maxHeight"
                    min="10"
                    step="5"
                    value={maxHeight}
                    onChange={(e) => onMaxHeightChange(Number(e.target.value))}
                    className={`w-full p-2 border bg-slate-800 rounded-lg focus:ring-2 text-sm font-mono text-white placeholder-slate-400
                        ${isLimitMode ? 'border-red-500 focus:ring-red-500' : 'border-slate-600 focus:ring-blue-500'}
                    `}
                />
                <span className="text-xs text-slate-400 font-mono">unités</span>
            </div>
            {isLimitMode && (
                <p className="text-[10px] text-red-300 italic">
                    Cliquez sur la grille pour appliquer/enlever cette limite.
                </p>
            )}
             {isTargetMode && (
                <p className="text-[10px] text-emerald-300 italic">
                    Cliquez pour marquer/démarquer les emplacements verts.
                </p>
            )}
        </div>

        {/* Hangar Management */}
        <div className="space-y-2 pt-2 border-t border-slate-700">
             <div className="flex items-center gap-2 text-slate-300 mb-2">
                <Archive size={14} />
                <span className="text-xs font-bold uppercase">Gestion des Hangars</span>
             </div>
             <div className="grid grid-cols-2 gap-2">
                 <button 
                    onClick={onExportHangar}
                    className="flex items-center justify-center gap-2 bg-slate-700 hover:bg-slate-600 text-slate-200 py-2 px-3 rounded text-xs transition-colors"
                 >
                     <Download size={14} /> Exporter
                 </button>
                 <button 
                    onClick={() => fileInputRef.current?.click()}
                    className="flex items-center justify-center gap-2 bg-slate-700 hover:bg-slate-600 text-slate-200 py-2 px-3 rounded text-xs transition-colors"
                 >
                     <Upload size={14} /> Importer
                 </button>
                 <input 
                    type="file" 
                    ref={fileInputRef} 
                    onChange={onImportHangar} 
                    className="hidden" 
                    accept=".json"
                 />
             </div>
        </div>

        {/* Input Heights */}
        <div className={`space-y-2 mt-4 pt-4 border-t border-slate-700 transition-opacity duration-200 ${isAnyPaintMode ? 'opacity-50 pointer-events-none' : 'opacity-100'}`}>
            <label htmlFor="heights" className="block text-sm font-semibold text-slate-300">
            Liste des hauteurs (Inventaire)
            </label>
            <div className="flex gap-2">
                <textarea
                    id="heights"
                    className="w-full h-16 p-3 border border-slate-600 bg-slate-800 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 text-sm font-mono text-white placeholder-slate-400 resize-none"
                    placeholder="Ex: 10 20 15 30"
                    value={inputString}
                    onChange={(e) => onInputChange(e.target.value)}
                    disabled={isAnyPaintMode}
                />
                <button
                    onClick={onApply}
                    disabled={isAnyPaintMode}
                    className="flex-shrink-0 w-16 flex items-center justify-center bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-colors shadow-sm"
                    title="Recharger les pavés"
                >
                    <RotateCcw size={24} className="animate-none hover:rotate-180 transition-transform duration-500" />
                </button>
            </div>
        </div>
      </div>

      {/* Palette Section */}
      <div className={`space-y-4 transition-opacity duration-200 ${isAnyPaintMode ? 'opacity-50 pointer-events-none' : 'opacity-100'}`}>
        <div className="flex items-center justify-between border-b border-slate-700 pb-2">
            <div className="flex items-center gap-2">
                <h2 className="text-sm font-bold text-white uppercase tracking-wide">Cubes</h2>
                <span className="text-xs text-slate-400">({unplacedBlocks.length})</span>
            </div>
            
            <button 
                onClick={onAutoStack}
                className="flex items-center gap-1.5 text-xs bg-emerald-600 hover:bg-emerald-700 text-white px-2 py-1 rounded transition-colors"
                title="Placer automatiquement tous les blocs disponibles"
            >
                <Wand2 size={12} />
                Organiser Auto
            </button>
        </div>
        
        {/* Inventory Content Split */}
        {sortedUnplaced.length === 0 ? (
            <div className="col-span-3 text-xs text-slate-500 text-center py-4 border border-dashed border-slate-700 rounded-lg">
                Inventaire vide
            </div>
        ) : (
            <div className="space-y-6">
                {/* 1. Oversized Blocks (Warning) */}
                {oversizedBlocks.length > 0 && renderBlockGrid(oversizedBlocks, "Pavés Hors Gabarit (> Max)", true)}
                
                {/* 2. Standard Blocks */}
                {standardBlocks.length > 0 && renderBlockGrid(standardBlocks, "Pavés Standards (Hauteur de base)")}
                
                {standardBlocks.length === 0 && oversizedBlocks.length === 0 && (
                     <p className="text-xs text-slate-500">Erreur d'affichage</p>
                )}
            </div>
        )}
      </div>

      {/* Stats */}
      <div className="pt-4 mt-auto">
          <div className="bg-slate-800 rounded-lg p-3 border border-slate-700">
              <div className="flex justify-between text-xs mb-1">
                  <span className="text-slate-400">Progression</span>
                  <span className="font-medium text-slate-200">{placedBlocks.length} / {blocks.length}</span>
              </div>
              <div className="w-full bg-slate-700 rounded-full h-1.5">
                  <div 
                    className="bg-green-500 h-1.5 rounded-full transition-all duration-500" 
                    style={{ width: `${blocks.length ? (placedBlocks.length / blocks.length) * 100 : 0}%` }}
                  ></div>
              </div>
          </div>
      </div>
    </div>
  );
};

export default Controls;