import { ClipboardList, Hourglass, Inbox, Truck, type LucideIcon } from 'lucide-react';
import type { JobStatusSummary } from '@/types';

export interface JobStatusCardItem {
  key: keyof JobStatusSummary;
  label: string;
  count: number;
  badgeClassName: string;
  icon: LucideIcon;
}

export function toJobStatusCardItems(summary: JobStatusSummary): JobStatusCardItem[] {
  return [
    { key: 'pending', label: 'Pending', count: summary.pending, badgeClassName: 'bg-job-pending', icon: Hourglass },
    { key: 'assigned', label: 'Assigned', count: summary.assigned, badgeClassName: 'bg-job-assigned', icon: ClipboardList },
    { key: 'enRoute', label: 'EnRoute', count: summary.enRoute, badgeClassName: 'bg-job-enroute', icon: Truck },
    { key: 'received', label: 'Received', count: summary.received, badgeClassName: 'bg-job-received', icon: Inbox },
  ];
}
