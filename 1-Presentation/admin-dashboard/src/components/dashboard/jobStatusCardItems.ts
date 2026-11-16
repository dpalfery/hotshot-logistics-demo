import { ClipboardList, Hourglass, Inbox, Truck } from 'lucide-react';
import type { JobStatusSummary } from '@/types';
import type { JobStatusCardItem } from './JobStatusCards';

/** Map validated API totals to the dashboard's ordered card presentation. */
export function buildJobStatusCardItems(summary: JobStatusSummary): JobStatusCardItem[] {
  return [
    { key: 'pending', label: 'Pending', count: summary.pending, color: 'bg-job-status-pending', icon: Hourglass },
    { key: 'assigned', label: 'Assigned', count: summary.assigned, color: 'bg-job-status-assigned', icon: ClipboardList },
    { key: 'enroute', label: 'EnRoute', count: summary.enRoute, color: 'bg-job-status-enroute', icon: Truck },
    { key: 'received', label: 'Received', count: summary.received, color: 'bg-job-status-received', icon: Inbox },
  ];
}
