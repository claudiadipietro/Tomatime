export interface Dashboard {
  pomoBalance: number;
  nextSession: { id: number; plannedStartTime: string; duration: number } | null;
  completedSessions: number;
  focusMinutes: number;
  activeSessions: { id: number; actualStartTime: string; duration: number }[];
}
