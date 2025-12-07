export interface Block {
  id: string;
  height: number;
  placedAt?: { x: number, y: number, z: number } | null; // Added z for stacking order
}

export interface GridSlot {
  x: number;
  y: number;
}

export const GRID_ROWS = 20;
export const GRID_COLS = 20;

// Constants for visualization
export const BLOCK_SIZE = 50; // Logical size of the base width/depth
export const ISO_ANGLE = 30 * (Math.PI / 180);