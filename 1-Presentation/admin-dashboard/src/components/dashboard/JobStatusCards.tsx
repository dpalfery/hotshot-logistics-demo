import type { JobStatusCardItem } from './jobStatusCardDefinitions';

/** Displays status counts supplied by the caller, without fetching data. */
export function JobStatusCards({ items }: { items: JobStatusCardItem[] }) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4" data-testid="job-status-cards">
      {items.map(item => (
        <div key={item.key} className="bg-white p-6 rounded-lg shadow" data-testid="job-status-card" data-status={item.key}>
          <div className="flex items-center gap-4">
            <div className={`w-8 h-8 rounded-md flex items-center justify-center text-white ${item.badgeClassName}`}>
              <item.icon aria-hidden="true" size={18} />
            </div>
            <span className="text-sm font-semibold">{item.label}</span>
          </div>
          <p className="text-2xl font-bold text-center" data-testid={`job-status-count-${item.key}`}>{item.count}</p>
        </div>
      ))}
    </div>
  );
}
