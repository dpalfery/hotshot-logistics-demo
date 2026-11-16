import { Hourglass, ClipboardList, Truck, Inbox } from 'lucide-react';
import type { JobStatusCounts } from '@/types';

export const jobStatusCardConfig = [
  { key: 'pending', label: 'Pending', color: '#3B82F6', icon: Hourglass },
  { key: 'assigned', label: 'Assigned', color: '#0EA5E9', icon: ClipboardList },
  { key: 'enRoute', label: 'EnRoute', color: '#0284C7', icon: Truck },
  { key: 'received', label: 'Received', color: '#0369A1', icon: Inbox },
] satisfies { key: keyof JobStatusCounts; label: string; color: string; icon: typeof Hourglass }[];
