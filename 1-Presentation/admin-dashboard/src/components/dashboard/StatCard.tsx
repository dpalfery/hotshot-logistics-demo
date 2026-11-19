export type StatValue =
  | { status: 'loading' }
  | { status: 'error' }
  | { status: 'data'; value: number };

interface StatCardProps {
  label: string;
  initial: string;
  iconClassName: string;
  testId: string;
  stat: StatValue;
}

/** One statistic. It shows its own loading, error or data state, so it never waits for another card. */
export function StatCard({ label, initial, iconClassName, testId, stat }: StatCardProps) {
  return (
    <div className="bg-white p-6 rounded-lg shadow" data-testid="stats-card">
      <div className="flex items-center">
        <div className="flex-shrink-0">
          <div className={`w-8 h-8 ${iconClassName} rounded-md flex items-center justify-center`}>
            <span className="text-white text-sm font-bold">{initial}</span>
          </div>
        </div>
        <div className="ml-4">
          <dt className="text-sm font-medium text-gray-500 truncate">{label}</dt>
          {stat.status === 'loading' && (
            <dd className="mt-1 h-8 w-12 animate-pulse rounded bg-gray-200" data-testid="loading-spinner" />
          )}
          {stat.status === 'error' && (
            <dd className="text-sm font-medium text-red-600">Failed to load</dd>
          )}
          {stat.status === 'data' && (
            <dd className="text-2xl font-semibold text-gray-900" data-testid={testId}>{stat.value}</dd>
          )}
        </div>
      </div>
    </div>
  );
}
