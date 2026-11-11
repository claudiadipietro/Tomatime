export interface Dashboard {
  pomoBalance: number;
  nextSession: { plannedStartTime: string; duration: number } | null;
  completedSessions: number;
  focusMinutes: number;
}
