import React, { useState, useRef } from 'react';
import { Maximize2, Minimize2 } from 'lucide-react';

interface ViewContainerProps {
  title: string;
  icon?: React.ReactNode;
  children: React.ReactNode;
}

const ViewContainer: React.FC<ViewContainerProps> = ({ title, icon, children }) => {
  const [isFullscreen, setIsFullscreen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const toggleFullscreen = () => {
    setIsFullscreen(!isFullscreen);
  };

  return (
    <div 
      ref={containerRef}
      className={`
        bg-white rounded-2xl shadow-sm border border-slate-200 flex flex-col overflow-hidden transition-all duration-300
        ${isFullscreen ? 'fixed inset-0 z-50 rounded-none' : 'relative h-[500px] xl:h-[600px]'}
      `}
    >
      <div className="flex items-center justify-between px-4 py-3 border-b border-slate-100 bg-slate-50/50 backdrop-blur-sm">
        <div className="flex items-center gap-2 text-slate-700 font-semibold text-sm">
          {icon}
          <span>{title}</span>
        </div>
        <button 
          onClick={toggleFullscreen}
          className="p-1.5 hover:bg-slate-200 text-slate-500 rounded-md transition-colors focus:outline-none focus:ring-2 focus:ring-slate-300"
          title={isFullscreen ? "Quitter plein écran" : "Plein écran"}
        >
          {isFullscreen ? <Minimize2 size={18} /> : <Maximize2 size={18} />}
        </button>
      </div>
      
      <div className="flex-1 relative overflow-hidden bg-slate-50/30 flex items-center justify-center p-4">
        {children}
      </div>
    </div>
  );
};

export default ViewContainer;