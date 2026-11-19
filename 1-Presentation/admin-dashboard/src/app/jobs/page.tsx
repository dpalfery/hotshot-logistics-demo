import { Suspense } from 'react';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { JobsManagement } from '@/components/jobs/JobsManagement';

export default function JobsPage() {
  return (
    <DashboardLayout>
      {/* JobsManagement reads the query string, which needs a Suspense boundary in a statically rendered page */}
      <Suspense fallback={<div className="text-center py-8">Loading jobs...</div>}>
        <JobsManagement />
      </Suspense>
    </DashboardLayout>
  );
}
