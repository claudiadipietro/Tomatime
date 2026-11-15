export interface CalendarSession {
  id: number;
  duration: number;
  status: 'Planned' | 'InProgress' | 'Completed' | 'Cancelled';
  plannedStartTime: string;
  actualStartTime: string | null;
  pomosEarned: number;
}
