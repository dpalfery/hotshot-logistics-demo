import type { LucideIcon } from 'lucide-react';

export interface JobStatusCardProps {
  label: string;
  count: number | null;
  color: string;
  icon: LucideIcon;
  isLoading?: boolean;
}

/** Displays one status and its count; null represents unavailable data. */
export function JobStatusCard({ label, count, color, icon: Icon, isLoading = false }: JobStatusCardProps) {
  return (
    <div className="bg-white p-6 rounded-lg shadow" data-testid="job-status-card" data-status={label}>
      <div className="flex items-center">
        <div className="w-8 h-8 rounded-md flex items-center justify-center" style={{ backgroundColor: color }} data-testid="job-status-badge">
          <Icon className="text-white" aria-hidden="true" />
        </div>
        <span className="ml-4 text-sm font-semibold text-gray-700">{label}</span>
      </div>
      <div className="text-2xl font-bold text-gray-900 text-center" data-testid="job-status-count">{isLoading ? '…' : count ?? '—'}</div>
    </div>
  );
}
