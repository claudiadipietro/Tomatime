export interface SessionHistoryItem {
  id: number;
  duration: number;
  status: 'Completed' | 'Cancelled';
  actualStartTime: string | null;
  plannedStartTime?: string;
  endedAt: string | null;
  pomosEarned: number;
}
