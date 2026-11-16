import type { LucideIcon } from 'lucide-react';

export interface JobStatusCardItem {
  key: string;
  label: string;
  count: number;
  color: string;
  icon: LucideIcon;
}

/** Render the supplied status totals in their given order. */
export function JobStatusCards({ items }: { items: JobStatusCardItem[] }) {
  return (
    <dl data-testid="job-status-cards" className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
      {items.map(({ key, label, count, color, icon: Icon }) => (
        <div key={key} data-testid="job-status-card" data-status={label} className="bg-white p-6 rounded-lg shadow">
          <dt className="flex items-center">
            <span className={`w-8 h-8 rounded-md flex items-center justify-center ${color}`}>
              <Icon className="h-5 w-5 text-white" aria-hidden="true" />
            </span>
            <span className="ml-3 text-sm font-semibold text-gray-700">{label}</span>
          </dt>
          <dd data-testid={`job-status-count-${key}`} className="mt-4 text-center text-2xl font-bold text-gray-900">{count}</dd>
        </div>
      ))}
    </dl>
  );
}
