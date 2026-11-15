export interface SessionHistoryItem {
  id: number;
  duration: number;
  status: 'Completed' | 'Cancelled';
  actualStartTime: string | null;
  endedAt: string | null;
  pomosEarned: number;
}
