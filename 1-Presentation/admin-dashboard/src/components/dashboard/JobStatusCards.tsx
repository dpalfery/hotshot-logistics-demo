import { JobStatusCard, type JobStatusCardProps } from './JobStatusCard';

interface JobStatusCardsProps {
  cards: JobStatusCardProps[];
  isLoading?: boolean;
  isError?: boolean;
}

/** Renders a responsive status grid and its loading or unavailable state. */
export function JobStatusCards({ cards, isLoading = false, isError = false }: JobStatusCardsProps) {
  return (
    <section aria-label="Job status counts" aria-busy={isLoading}>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4" data-testid="job-status-cards">
        {cards.map(card => (
          <JobStatusCard key={card.label} {...card} isLoading={isLoading} count={isError ? null : card.count} />
        ))}
      </div>
      {isError && <p role="alert">Job status counts unavailable</p>}
    </section>
  );
}
