export interface Dashboard {
  pomoBalance: number;
  nextSession: { id: number; plannedStartTime: string; duration: number } | null;
  completedSessions: number;
  focusMinutes: number;
}
