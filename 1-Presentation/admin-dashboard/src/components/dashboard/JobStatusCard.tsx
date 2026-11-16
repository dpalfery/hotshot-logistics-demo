import type { ReactNode } from 'react';

interface JobStatusCardProps {
  label: string;
  count: number;
  colorClass: string;
  icon: ReactNode;
}

/** Displays one job status and its API-provided count. */
export function JobStatusCard({ label, count, colorClass, icon }: JobStatusCardProps) {
  return (
    <div className="bg-white p-6 rounded-lg shadow" data-testid="job-status-card">
      <div className="flex items-center gap-4">
        <div className={`w-8 h-8 rounded-md flex items-center justify-center text-white ${colorClass}`} aria-hidden="true">
          {icon}
        </div>
        <p className="text-sm font-semibold text-gray-500">{label}</p>
      </div>
      <p className="text-2xl font-bold text-gray-900 text-center">{count}</p>
    </div>
  );
}
