import { ClipboardList, Hourglass, Inbox, Truck } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';

export interface JobStatusCard {
  label: string;
  count: number;
  color: string;
  icon: string;
}

const icons: Record<string, LucideIcon> = { Hourglass, ClipboardList, Truck, Inbox };

/** Displays supplied job status counts without fetching data. */
export default function JobStatusCards({ statuses }: { statuses: JobStatusCard[] }) {
  return (
    <div data-testid="job-status-cards" className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      {statuses.map(({ label, count, color, icon }) => {
        const key = label.replace(/\s/g, '').toLowerCase();
        const Icon = icons[icon];
        return (
          <div key={key} data-testid={`job-status-card-${key}`} className="bg-white p-6 rounded-lg shadow">
            <div className="flex items-center gap-4">
              <div
                data-testid={`job-status-badge-${key}`}
                className="w-8 h-8 rounded-md flex items-center justify-center"
                style={{ backgroundColor: color }}
              >
                {Icon && <Icon className="h-5 w-5 text-white" aria-hidden="true" />}
              </div>
              <span className="text-sm font-semibold text-gray-700">{label}</span>
            </div>
            <div data-testid={`job-status-count-${key}`} className="text-2xl font-bold text-gray-900 text-center">
              {count}
            </div>
          </div>
        );
      })}
    </div>
  );
}
